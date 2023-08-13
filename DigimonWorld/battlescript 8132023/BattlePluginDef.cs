// @@version 1.3.2.4
// @@link mscorlib.dll
// @@link System.Runtime.dll
// @@link System.Linq.dll
// @@link System.Windows.Forms.dll
// @@link System.Net.dll
// @@link System.dll
// @@link System.Core.dll
// @@link System.Drawing.dll
// @@link System.Collections.dll
// @@include BattleActor.cs
// @@include BattleCameraController.cs
// @@include BattleContentGetter.cs
// @@include BattleDamageTextInfo.cs
// @@include BattleEventController.cs
// @@include BattleSequenceManager.cs
// @@include BattleViewer.cs
// @@include BattleViewer3D.cs
// @@include BattleViewer3DPreview.cs
// @@include CommandTargetSelector.cs
// @@include RecoveryStatusInfo.cs
// @@include ResultViewer.cs

using Yukar.Engine;
using Yukar.Engine.GameContentCreatorSub;

namespace Yukar.Battle
{
    /// <summary>
    /// バトルプラグインのエントリポイント
    /// Battle plugin entry point
    /// </summary>
    public class BattlePluginDef
    {
        public static void SetFactory()
        {
            BattleSequenceManagerBase.create = (owner, catalog) => new BattleSequenceManager(owner, catalog);
            BattleViewer3DPreviewBase.create = (infoGetter, catalog, btlSett) => new BattleViewer3DPreview(infoGetter, catalog, btlSett);
            BattleContentGetterBase.create = () => new BattleContentGetter();
        }
    }
}
