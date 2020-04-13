using System;
using System.Linq;
using System.Reflection;
using System.Diagnostics;
using NUnit.Framework;
using UnityEngine;
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

        static readonly int ply=3;
        [Test]
        public void Perft()
        {
            var thomas = new Engine(classicFEN);
            var sw = new Stopwatch();

            sw.Start();
            int nodes = thomas.Perft(3);
            sw.Stop();

            Assert.IsTrue(nodes==8902);

            MonoBehaviour.print($"n={nodes} t={sw.Elapsed}");
        }
        [Test]
        public void Evaluate()
        {
            var thomas = new Engine(kiwiPete);
            var sw = new Stopwatch();

            sw.Start();
            var evals = thomas.EvaluatePosition(2);
            sw.Stop();
            foreach (var kvp in evals)
            {
                MonoBehaviour.print($"{kvp.Key} {kvp.Value}");
            }
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
    }
}
