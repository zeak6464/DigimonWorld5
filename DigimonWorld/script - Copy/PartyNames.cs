using Microsoft.Xna.Framework;
using SharpKmyMath;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Yukar.Common.GameData;
using Yukar.Common.Rom;
using Yukar.Engine;
using static Yukar.Engine.MapData;
using Hero = Yukar.Common.GameData.Hero;

namespace Bakin
{
  public class PartyNames : BakinObject
  {
	private SystemData _systemData;
    private readonly string currentPartyName = "cParty"; //current party - Bakin variable name
    private readonly string partyName = "party"; //Static party - Bakin variable name


    public override void Start()
    {
      // キャラクターが生成される時に、このメソッドがコールされます。
      // This method is called when the character is created.
    }

    public override void Update()
    {
      if (mapScene == null) { mapScene = GameMain.instance.mapScene; return; }

      List<Hero> partyMembers = GameMain.instance.data.party.Players;

      if (partyMembers == null) return;

      // Get the names of all the player characters in the party.
      string[] partyNames = new string[partyMembers.Count];
      for (int i = 0; i < partyMembers.Count; i++)
      {
        partyNames[i] = partyMembers[i].Name;
      }

      // Set the values of the Bakin variables for the current party and static party names.
      mapScene.owner.data.system.SetVariable(currentPartyName, partyNames, Guid.Empty, false);
      mapScene.owner.data.system.SetVariable(partyName, partyNames, Guid.Empty, false);

      // ARRAY VERSION:
      // mapScene.owner.data.system.SetToArray(currentPartyName, i, currentName);
      // mapScene.owner.data.system.SetToArray(partyName, i, party[i].rom.name);
         
      }

    public override void BeforeUpdate()
    {
      // キャラクターが生存している間、
      // 毎フレーム、イベント内容の実行前にこのメソッドがコールされます。
      // This method will be called every frame while the character is alive, before the event content is executed.
    }

    public override void Destroy()
    {
      // キャラクターが破棄される時に、このメソッドがコールされます。
      // This method is called when the character is destroyed.
    }

    public override void AfterDraw()
    {
      // このフレームの2D描画処理の最後に、このメソッドがコールされます。
      // This method is called at the end of the 2D drawing process for this frame.
    }
  }
}
