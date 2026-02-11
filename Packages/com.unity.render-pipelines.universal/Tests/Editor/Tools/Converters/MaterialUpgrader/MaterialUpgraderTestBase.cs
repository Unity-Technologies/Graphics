using System;
using System.Collections;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Rendering;
using UnityEditor.Rendering.Universal;
using UnityEngine;
using UnityEngine.TestTools;


abstract class MaterialUpgraderTestBase<T> where T : MaterialUpgrader
{
    protected T m_Upgrader;
    protected Material m_Material;

    protected string m_OldShaderPath;
    protected string m_NewShaderPath;

    protected MaterialUpgraderTestBase(string oldShaderPath, string newShaderPath)
    {
        m_OldShaderPath = oldShaderPath;
        m_NewShaderPath = newShaderPath;
    }

    [OneTimeSetUp]
    public abstract void OneTimeSetUp();

    [SetUp]
    public void Setup()
    {
        var shader = Shader.Find(m_OldShaderPath);
        Assume.That(shader, Is.Not.Null, $"Shader '{m_OldShaderPath}' not found.");
        m_Material = new Material(shader);
    }

    [TearDown]
    public void TearDown()
    {
        if (m_Material != null)
        {
            UnityEngine.Object.DestroyImmediate(m_Material);
            m_Material = null;
        }
    }

    [OneTimeTearDown]
    public void OneTimeTearDown()
    {
        m_Upgrader = null;
    }

    public class MaterialUpgradeTestCase
    {
        public string name;
        public Action<Material> setup;
        public Action<Material> verify;
        public bool ignore;
        public string ignoreReason;
        public override string ToString() => name;
    }

    protected void UpgradeMaterial(MaterialUpgradeTestCase testCase)
    {
        if (testCase.ignore)
        {
            Assert.Ignore(testCase.ignoreReason ?? "Test case ignored.");
        }

        Assume.That(testCase.setup, Is.Not.Null, $"Test case '{testCase.name}' has a null setup Action.");
        Assume.That(testCase.verify, Is.Not.Null, $"Test case '{testCase.name}' has a null verify Action.");

        // Arrange
        var material = m_Material;
        testCase.setup(material);

        // Act
        m_Upgrader.Upgrade(material, MaterialUpgrader.UpgradeFlags.None);

        //Assert common
        Assert.AreEqual(m_NewShaderPath, material.shader.name, $"Shader mismatch in case: {testCase.name}");

        //Assert specific
        testCase.verify(material);
    }
}

