using Avalonia;
using Linguistics.App.Controls;

namespace Linguistics.App.Tests;

[TestClass]
public sealed class PaperStageTests
{
    [TestMethod]
    public void StageLayerEnumKeepsTheApprovedNineLayerOrder()
    {
        CollectionAssert.AreEqual(
            new[]
            {
                PaperStageLayer.Backdrop,
                PaperStageLayer.PaperWash,
                PaperStageLayer.SupportingCast,
                PaperStageLayer.AmbientPieces,
                PaperStageLayer.TapedLabel,
                PaperStageLayer.ForegroundSilhouettes,
                PaperStageLayer.Subject,
                PaperStageLayer.ReactionBurst,
                PaperStageLayer.VerdictCard,
            },
            Enum.GetValues<PaperStageLayer>());

        var zIndexes = Enum
            .GetValues<PaperStageLayer>()
            .Select(PaperStage.GetLayerZIndex)
            .ToArray();
        CollectionAssert.AreEqual(Enumerable.Range(0, 9).ToArray(), zIndexes);
    }

    [TestMethod]
    public void AnatomicalAnchorLinesProgressFromHeadToFoot()
    {
        var ratios = new[]
        {
            PaperStage.GetAnchorRatio(PaperAnchorLine.Head),
            PaperStage.GetAnchorRatio(PaperAnchorLine.Shoulder),
            PaperStage.GetAnchorRatio(PaperAnchorLine.Waist),
            PaperStage.GetAnchorRatio(PaperAnchorLine.Foot),
        };

        CollectionAssert.AreEqual(new[] { 0.18, 0.34, 0.58, 0.88 }, ratios);
        Assert.IsTrue(ratios.SequenceEqual(ratios.Order()));
    }

    [TestMethod]
    public void FootAnchoredPuppetPlacesItsBottomOnTheFootLine()
    {
        var bounds = PaperStage.CalculateAnchoredBounds(
            new Size(1000, 500),
            new Size(120, 200),
            PaperAnchorLine.Foot,
            anchorX: 0.5,
            offset: default);

        Assert.AreEqual(440, bounds.Bottom);
        Assert.AreEqual(500, bounds.Center.X);
    }

    [TestMethod]
    public void AnchorOffsetsAndHorizontalPositionAreAppliedWithoutClampingTheScenePiece()
    {
        var bounds = PaperStage.CalculateAnchoredBounds(
            new Size(800, 400),
            new Size(100, 80),
            PaperAnchorLine.Head,
            anchorX: 0.25,
            offset: new Vector(-20, 12));

        Assert.AreEqual(130, bounds.X);
        Assert.AreEqual(84, bounds.Y);
    }
}
