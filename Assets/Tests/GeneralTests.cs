using System;
using System.Linq;
using System.Reflection;
using System.Diagnostics;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using System.Text;
using UnityEngine.TestTools;

namespace Tests
{
    public class GeneralTests
    {
        static readonly string classicFEN = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w AHah - 0 1";
        static readonly string kiwiPete = "r3k2r/p1ppqpb1/bn2pnp1/3PN3/1p2P3/2N2Q1p/PPPBBPPP/R3K2R w AHah - 0 1";
        static readonly string position3 = "8/2p5/3p4/KP5r/1R3p1k/8/4P1P1/8 w - - 0 1";
        static readonly string position4 = "r3k2r/Pppp1ppp/1b3nbN/nP6/BBP1P3/q4N2/Pp1P2PP/R2Q1RK1 w ah - 0 1";
        static readonly string position5 = "rnbq1k1r/pp1Pbppp/2p5/8/2B5/8/PPP1NnPP/RNBQK2R w AH - 1 8";

        [Test]
        public void Perft()
        {
            void DoPerft(string FEN, int ply, int nodes)
            {
                var thomas = new Engine(FEN);
                var sw = new Stopwatch();
                sw.Start();
                int nodess = thomas.Perft(ply);
                sw.Stop();
                Assert.IsTrue(nodess==nodes);
                MonoBehaviour.print($"n={nodes} t={sw.Elapsed}");
            }
            DoPerft(classicFEN, 4, 197281);
            DoPerft(kiwiPete, 3, 97862);
            DoPerft(position3, 4, 43238);
            DoPerft(position4, 3, 9467);
            DoPerft(position5, 3, 62379);
        }
        [Test]
        public void Evaluate()
        {
            void DoEval(string FEN, int nmPly, int qPly)
            {
                var thomas = new Engine(FEN);
                var sw = new Stopwatch();
                sw.Start();
                var evals = thomas.EvaluatePosition(nmPly, qPly);
                sw.Stop();
                var bob = new StringBuilder();
                foreach (var kvp in evals)
                {
                    bob.Append($"{kvp.Key} {kvp.Value/100}, ");
                }
                bob.Append($"\n{sw.Elapsed}");
                MonoBehaviour.print(bob.ToString());
            }

            DoEval(kiwiPete, 1, 2);
            DoEval(kiwiPete, 2, 2);
            // DoEval("kr/2/2/KR w - - 0 1", 1);
            // DoEval("kr/2/2/KR w - - 0 1", 2);
        }
        [Test]
        public void PlayAgainstRandom()
        {
            // TODO:
        }

        [Test]
        public void SceneSerializeFieldsNotNull()
        {
            foreach (var monoB in MonoBehaviour.FindObjectsOfType<MonoBehaviour>())
            {
                AssertSerializeFieldsNotNull(monoB);
            }
        }
        private void AssertSerializeFieldsNotNull(MonoBehaviour monoB)
        {
            Type type = monoB.GetType();
            // Assert.IsNotNull(type, $"{monoB.name} is null type");
            if (type.Namespace!=null && (type.Namespace.Contains("UnityEngine") || type.Namespace.Contains("TMPro"))) {
                return;
            }
            foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                                        .Where(f=> Attribute.IsDefined(f, typeof(SerializeField))))
            {
                // MonoBehaviour.print($"{type.Name} {field.Name} {field.GetValue(monoB)}");
                Assert.IsNotNull(field.GetValue(monoB), $"SerializeField {field.Name} in {monoB.name} is null");
            }
        }
        [Test]
        public void NavigationDisabled()
        {
            foreach (var selectable in MonoBehaviour.FindObjectsOfType<Selectable>())
            {
                Assert.IsTrue(selectable.navigation.mode == Navigation.Mode
                .None, $"{selectable.name} has navigation enabled");
            }
        }
    }
}
