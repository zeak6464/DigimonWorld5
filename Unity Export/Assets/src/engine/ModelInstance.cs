namespace Yukar.Engine
{
    public class ModifiedModelInstance
	{
		public SharpKmyGfx.ModelInstance inst;
		public ModifiedModelData modifiedModel;

		float[] uscroll = new float[32];
		float[] vscroll = new float[32];
		float[] stopanimTime = new float[32];

		public ModifiedModelInstance(ModifiedModelData data)
		{
			modifiedModel = data;
			inst = new SharpKmyGfx.ModelInstance(data.model);

			int didx = 0;
			var baseshd = SharpKmyGfx.Shader.load("map");
			while (true)
			{
				SharpKmyGfx.DrawInfo di = inst.getDrawInfo(didx);
				if (di == null) break;

				di.setShader(baseshd);
				int mtlid = inst.getMeshMaterialIndex(didx);

				foreach(int sid in data.shader.Keys)
				{
					if (sid == mtlid)
					{
						SharpKmyGfx.Shader shd = SharpKmyGfx.Shader.load(data.shader[sid]);
						if (shd != null)
						{
							di.setShader(shd);
							shd.Release();
						}
					}
				}
				foreach (int sid in data.depthWrite.Keys)
				{
					if (sid == mtlid)
					{
						di.setDepthWrite(data.depthWrite[sid]);
					}
				}

				didx++;
			}
			baseshd.Release();

			for(int i = 0;i < data.motions.Count;i++)
			{
				inst.addMotion(data.motions[i].name, data.model, data.motions[i].start, data.motions[i].end, data.motions[i].loop);
			}

            if (data.motions.Count > 0)
                inst.playMotion(data.motions[0].name, 0);

			for (int i = 0; i < 32; i++) stopanimTime[i] = 0;
		}

		public void update()
		{
			for (int midx = 0; midx < 32; midx++)
			{
				if (modifiedModel.stopanimFrames[midx] != 0)
				{
					stopanimTime[midx] += GameMain.getElapsedTime();
					int idx = (int)((stopanimTime[midx] / modifiedModel.stopanimInterval[midx])) % modifiedModel.stopanimFrames[midx];
					int uidx = idx % modifiedModel.stopanimU[midx];
					int vidx = (idx / modifiedModel.stopanimU[midx]) % modifiedModel.stopanimV[midx];

					uscroll[midx] = (float)uidx / modifiedModel.stopanimU[midx];
					vscroll[midx] = 1.0f - (float)vidx / modifiedModel.stopanimV[midx];
				}
				else
				{
					if (modifiedModel.uspeed[midx] != 0)
					{
						uscroll[midx] += modifiedModel.uspeed[midx] * GameMain.getElapsedTime();
					}
					if (modifiedModel.vspeed[midx] != 0)
					{
						vscroll[midx] += modifiedModel.vspeed[midx] * GameMain.getElapsedTime();
					}
				}
			}

			int didx = 0;
			while (true)
			{
				SharpKmyGfx.DrawInfo d = inst.getDrawInfo(didx);
				if (d == null) break;
				//d.setParam(mapobjects.IndexOf(p) + 1);
				MapData.setFogParam(d);
				int midx = inst.getMeshMaterialIndex(didx);
				d.setVPMatrix("texcoord0", SharpKmyMath.Matrix4.translate(uscroll[midx], vscroll[midx], 0));
				didx++;
			}
		}

		public void resetShader(string name)
		{
			var shd = SharpKmyGfx.Shader.load(name);
			if (shd != null)
			{
				int idx = 0;
				while (true)
				{
					var d = inst.getDrawInfo(idx);
					idx++;
					if (d == null) break;

					d.setShader(shd);
				}
			}
			shd.Release();
		}

		public void Release()
		{
			inst.Release();
		}

        internal void reapplyMtlScrollState()
        {
            int didx = 0;
            while (true)
            {
                SharpKmyGfx.DrawInfo d = inst.getDrawInfo(didx);
                if (d == null) break;
                int midx = inst.getMeshMaterialIndex(didx);
                d.setVPMatrix("texcoord0", SharpKmyMath.Matrix4.translate(uscroll[midx], vscroll[midx], 0));
                didx++;
            }
        }
    }
}
