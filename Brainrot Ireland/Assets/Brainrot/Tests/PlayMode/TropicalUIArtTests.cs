using NUnit.Framework;
using UnityEngine;
namespace Brainrot.Tests
{
    public sealed class TropicalUIArtTests
    {
        [Test]public void ReferenceSpritesUseRealAtlasAndStayInsideItsBounds(){
            var atlas=Resources.Load<Texture2D>("TropicalUIAtlas");Assert.IsNotNull(atlas);Assert.AreEqual(793,atlas.width,"NPOT atlas must not resize on import.");Assert.AreEqual(1983,atlas.height);
            foreach(string key in new[]{"VitalsFrame","Portrait","WavePlaque","StageRibbon","StatsStrip","HeavyCard","DashCard","ReadyBadge","HeavyIcon","DashIcon","PauseIcon","SoundIcon","WoodButton","WoodButtonGold","WoodPanel","ControlsPanel","LogoPlaque","HeaderPlaque","GoldButton","BlueButton","FlowerAccent","MenuPlay","MenuNew","MenuRecords","MenuSettings","MenuExit","ReferenceMainMenu","ReferencePause","ReferenceSettings","ReferenceMenuScene","IslandSandPanel","IslandTitle","IslandPrimary","IslandSecondary","IslandTeal","IslandSwitchOn","IslandSwitchOff","IslandBat","IslandHealth","IslandShoe","IslandStopwatch","IslandCard","IslandTrophy","IslandCoin","IslandShellKnob","IslandWaveLeft","IslandWaveRight","IslandSliderTrack","IslandSliderFill","IslandSlogan","IslandMenuScene","IslandSandTile","IslandBatBare","IslandHealthBare","IslandShoeBare","IslandStopwatchBare"}){
                var sprite=TropicalUIArt.Get(key);CollectionAssert.Contains(new[]{"TropicalUIAtlas","TropicalPanels","TropicalIcons","TropicalBoard","ReferenceMainMenu","ReferencePause","ReferenceSettings","TropicalMenuBackdrop","ReferenceMenuClean","ReferencePauseClean","ReferenceSettingsClean","WaveTitleClean","AlphaPauseFrame","AlphaSettingsFrame","AlphaButtons","IslandMenuAtlas","IslandControlsAtlas","IslandMenuBackdrop","IslandUpgradeIcons"},sprite.texture.name,"Atlas entry missing: "+key);Assert.Greater(sprite.rect.width,0);Assert.GreaterOrEqual(sprite.rect.xMin,0);Assert.LessOrEqual(sprite.rect.xMax,sprite.texture.width);Assert.GreaterOrEqual(sprite.rect.yMin,0);Assert.LessOrEqual(sprite.rect.yMax,sprite.texture.height);
            }
        }
        [TestCase("IslandMenuAtlas",1254,1254)]
        [TestCase("IslandControlsAtlas",1254,1254)]
        [TestCase("IslandMenuBackdrop",1672,941)]
        public void IllustratedMenuTexturesKeepSourceDimensions(string resource,int width,int height){
            var texture=Resources.Load<Texture2D>(resource);Assert.IsNotNull(texture,resource);
            Assert.AreEqual(width,texture.width,"Import must preserve JSON atlas coordinates.");
            Assert.AreEqual(height,texture.height);Assert.AreEqual(TextureWrapMode.Clamp,texture.wrapMode);
            Assert.AreEqual(1,texture.mipmapCount,"UI sprites should not allocate a mip chain.");
        }
    }
}
