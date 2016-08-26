using System;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using Weavers;

namespace Weaver.Tests
{
    [TestFixture]
    public class WeaverTest
    {
        [SetUp]
        public void SetUp()
        {
            var weavingTask = new ModuleWeaver();

            weavingTask.Execute();
        }

        [Test]
        public void ValidateDynamicProxyTypesAreCreated()
        {
            var folderPath = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..\\..\\..\\lib"));
            var assemblyName = "YodiiProxy.dll";
            var finalPath = Path.Combine(folderPath, assemblyName);

            var assembly = Assembly.LoadFile(finalPath);

            var types = assembly.DefinedTypes.ToList();
            Assert.AreEqual(types[0].Name, "IService_Proxy_1");
            Assert.AreEqual(types[1].Name, "IService_Proxy_1_UN");
        }
    }
}
