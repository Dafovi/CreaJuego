using System.IO;
using CreaJuego.Editor;
using NUnit.Framework;
namespace CreaJuego.Starter.Tests
{
    public sealed class EditorConfigurationTests
    {
        [Test] public void CanonicalLayoutIsResolvedFromThisProject()
        {
            Assert.That(WorkshopEditorConfiguration.LayoutPath,Is.EqualTo(Path.GetFullPath(Path.Combine(UnityEngine.Application.dataPath,"..","Docs","Layouts","CreaJuego-Taller.wlt"))));
            Assert.That(File.Exists(WorkshopEditorConfiguration.LayoutPath),Is.True);
        }
    }
}
