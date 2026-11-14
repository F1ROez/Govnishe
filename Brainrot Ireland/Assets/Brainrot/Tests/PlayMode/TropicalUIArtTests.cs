using NUnit.Framework;
using UnityEngine;
namespace Brainrot.Tests
{
    public sealed class TropicalUIArtTests
    {
        [Test]public void ReferenceSpritesUseRealAtlasAndStayInsideItsBounds(){
            var atlas=Resources.Load<Texture2D>("TropicalUIAtlas");Assert.IsNotNull(atlas);Assert.AreEqual(793,atlas.width,"NPOT atlas must not resize on import.");Assert.AreEqual(1983,atlas.height);
            foreach(string key in new[]{"VitalsFrame","Portrait","WavePlaque","StageRibbon","StatsStrip","HeavyCard","DashCard","ReadyBadge","HeavyIcon","DashIcon","PauseIcon","SoundIcon","WoodButton","WoodButtonGold","WoodPanel","ControlsPanel","LogoPlaque"}){
                var sprite=TropicalUIArt.Get(key);Assert.AreSame(atlas,sprite.texture,"Atlas entry missing: "+key);Assert.Greater(sprite.rect.width,0);Assert.GreaterOrEqual(sprite.rect.xMin,0);Assert.LessOrEqual(sprite.rect.xMax,atlas.width);Assert.GreaterOrEqual(sprite.rect.yMin,0);Assert.LessOrEqual(sprite.rect.yMax,atlas.height);
            }
        }
    }
}
