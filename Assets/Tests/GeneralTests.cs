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
        static readonly int ply=2;
        [Test]
        public void Perft()
        {
            var thomas = new Engine(classicFEN, 2, false);
            var sw = new Stopwatch();

            sw.Start();
            int nodes = thomas.Perft(2);
            sw.Stop();

            Assert.IsTrue(nodes==400);

            MonoBehaviour.print($"n={nodes} t={sw.Elapsed}");
        }
        [Test]
        public void Evaluate()
        {
            var thomas = new Engine(classicFEN, 2, false);
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
