using System;
using System.Collections.Generic;
using System.Linq;

namespace Yukar.Engine
{
    public class ModifiedModelData
	{
		public enum DEFREADMODE
		{
			kNONE,
			kANIM,
			kMTL,
            kPREVIEW,
		}

		public class MotionInfo
		{
			public string name;
			public int start;
			public int end;
			public bool loop;
            public string rootName;	// For Unity
        }

        public const int MAX_MATERIAL = 32;
		public float[] uspeed;
		public float[] vspeed;
		public int[] stopanimU;
		public int[] stopanimV;
		public int[] stopanimFrames;
		public float[] stopanimInterval;
		public float[] stopanimTime;
		public List<MotionInfo> motions;
		public SharpKmyGfx.ModelData model;
		public SharpKmyGfx.StaticModelBatcher batcher;
		public Dictionary<int,  string> shader;
		public Dictionary<int, bool> depthWrite;
		public int userCount = 0;
        public SharpKmyMath.Matrix4 previewMatrix = SharpKmyMath.Matrix4.identity();

#if WINDOWS
        private bool errorAlreadyShown = false;
#endif

		public ModifiedModelData()
		{
			uspeed = new float[MAX_MATERIAL];
			vspeed = new float[MAX_MATERIAL];
			stopanimU = new int[MAX_MATERIAL];
			stopanimV = new int[MAX_MATERIAL];
			stopanimFrames = new int[MAX_MATERIAL];
			stopanimInterval = new float[MAX_MATERIAL];
			stopanimTime = new float[MAX_MATERIAL];
			for (int i = 0; i < MAX_MATERIAL; i++)
			{
				uspeed[i] = 0;
				vspeed[i] = 0;
				stopanimU[i] = 0;
				stopanimV[i] = 0;
				stopanimFrames[i] = 0;
				stopanimInterval[i] = 0;
				stopanimTime[i] = 0;
			}
			motions = new List<MotionInfo>();
			batcher = null;
			shader = new Dictionary<int, string>();
			depthWrite = new Dictionary<int, bool>();
		}

		public void Release()
		{
			if(batcher != null)batcher.Release();
		}

		public void createBacher()
		{
			if (batcher == null)
			{
				var baseshd = SharpKmyGfx.Shader.load("map");
				batcher = new SharpKmyGfx.StaticModelBatcher(model, 0);
                if (!batcher.isAvailable())
                {
                    batcher = null;
                    return;
                }

				int didx = 0;
				while (true)
				{
					SharpKmyGfx.DrawInfo di = batcher.getDrawInfo(didx);
					if (di == null) break;
					di.setShader(baseshd);
					int mtlid = model.getMeshClusterMaterialIndex(didx);

					foreach (int sid in shader.Keys)
					{
						if (sid == mtlid)
						{
							SharpKmyGfx.Shader shd = SharpKmyGfx.Shader.load(shader[sid]);
							if (shd != null)
							{
								di.setShader(shd);
								shd.Release();
							}
						}
					}
					foreach (int sid in depthWrite.Keys)
					{
						if (sid == mtlid)
						{
							di.setDepthWrite(depthWrite[sid]);
						}
					}

					didx++;
				}
				baseshd.Release();

			}
			
		}

		public void init( SharpKmyGfx.ModelData mdl, string path)
		{
			model = mdl;

			string defpath = System.IO.Path.ChangeExtension(path, ".def");
			string mtlname = "";
			int mtlidx = -1;
			ModifiedModelData mmd = this;

			if (Common.Util.file.exists(defpath))
			{
				var reader = Common.Util.file.getStreamReader(defpath);
				DEFREADMODE mode = DEFREADMODE.kNONE;

				while (!reader.EndOfStream)
				{
					string line = reader.ReadLine();
					string[] words = line.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

					if (words.Length == 0) continue;
					if (words[0] == "anim")
					{
						mode = DEFREADMODE.kANIM;
					}
					else if (words[0] == "mtl" && words.Length >= 2)
					{
						mode = DEFREADMODE.kMTL;
						mtlname = words[1];
						if (mtlname.IndexOf("Material::") == 0)
						{
							mtlname = mtlname.Substring(10);
						}
						mtlidx = mdl.getMaterialIndex(mtlname);
                    }
                    else if (words[0] == "preview")
                    {
                        mode = DEFREADMODE.kPREVIEW;
                    }
					else
					{
						switch (mode)
						{
							case DEFREADMODE.kANIM:
                                Func<int, string> getWord = null;
                                getWord = (int index) =>
                                {
                                    if (words.Length <= index)
                                    {
                                        switch (index)
                                        {
                                            case 1:
                                                return "0";
                                            case 2:
                                                return getWord(1);
                                            case 3:
                                            default:
                                                return "";
                                        }
                                    }
                                    return words[index];
                                };
                                if (words.Length >= 2)
                                {
									motions.Add(new MotionInfo());
									motions.Last().name = getWord(0);
									int.TryParse(getWord(1), out motions.Last().start);
                                    motions.Last().end = motions.Last().start;
                                    int.TryParse(getWord(2), out motions.Last().end);
									motions.Last().loop = getWord(3) == "loop";
                                }
								break;

							case DEFREADMODE.kMTL:
								//必要ならマテリアル（シェーダー、ブレンドなど）設定
								if (words[0] == "blend" && words.Length >= 2)
								{
									if (words[1] == "add"){
										mdl.setBlendType(mtlname, 2);
									}else if(words[1] == "modulate"){
										mdl.setBlendType(mtlname, 4);
									}
								}
								else if(words[0] == "shader" && words.Length >= 2)
								{
                                    if(!shader.ContainsKey(mtlidx))
    									shader.Add(mtlidx, words[1]);
								}
								else if(words[0] == "depthwrite" && words.Length >= 2)
                                {
                                    if (!depthWrite.ContainsKey(mtlidx))
                                        depthWrite.Add(mtlidx, bool.Parse(words[1]));
								}
								else if (words[0] == "uscrollspeed" && words.Length >= 2)
								{
									if (mtlidx >= 0 && mtlidx < 32)
									{
                                        mmd.uspeed[mtlidx] = float.Parse(words[1], System.Globalization.CultureInfo.InvariantCulture);
                                    }
								}
								else if (words[0] == "vscrollspeed" && words.Length >= 2)
								{
									if (mtlidx >= 0 && mtlidx < 32)
									{
                                        mmd.vspeed[mtlidx] = float.Parse(words[1], System.Globalization.CultureInfo.InvariantCulture);
                                    }
								}
								else if (words[0] == "texstopanim" && words.Length >= 5)
								{
									if (mtlidx >= 0 && mtlidx < 32)
									{
										mmd.stopanimU[mtlidx] = int.Parse(words[1]);
										mmd.stopanimV[mtlidx] = int.Parse(words[2]);
										mmd.stopanimFrames[mtlidx] = int.Parse(words[3]);
                                        mmd.stopanimInterval[mtlidx] = float.Parse(words[4], System.Globalization.CultureInfo.InvariantCulture);

										if (mmd.stopanimU[mtlidx] <= 0 || mmd.stopanimV[mtlidx] <= 0 ||
											mmd.stopanimInterval[mtlidx] <= 0) mmd.stopanimFrames[mtlidx] = 0;
                                    }
								}
								break;

                            case DEFREADMODE.kPREVIEW:
                                if (words[0] == "angle" && words.Length >= 4)
                                {
                                    var previewAngle = new SharpKmyMath.Vector3(
                                        (float)(float.Parse(words[1], System.Globalization.CultureInfo.InvariantCulture) / 180 * Math.PI),
                                        (float)(float.Parse(words[2], System.Globalization.CultureInfo.InvariantCulture) / 180 * Math.PI),
                                        (float)(float.Parse(words[3], System.Globalization.CultureInfo.InvariantCulture) / 180 * Math.PI));
                                    
                                    previewMatrix *=
                                        SharpKmyMath.Matrix4.rotateX(previewAngle.x) *
                                        SharpKmyMath.Matrix4.rotateY(previewAngle.y) *
                                        SharpKmyMath.Matrix4.rotateZ(previewAngle.z);
                                }else if (words[0] == "offset" && words.Length >= 4)
                                {
                                    var previewOffset = new SharpKmyMath.Vector3(
                                        float.Parse(words[1], System.Globalization.CultureInfo.InvariantCulture),
                                        float.Parse(words[2], System.Globalization.CultureInfo.InvariantCulture),
                                        float.Parse(words[3], System.Globalization.CultureInfo.InvariantCulture));

                                    previewMatrix *=
                                        SharpKmyMath.Matrix4.translate(previewOffset.x, previewOffset.y, previewOffset.z);
                                }
                                break;
							case DEFREADMODE.kNONE:
								//無効な定義
								break;
						}
					}
                }

                reader.Close();
			}
			else
			{
				// 特になにもしない
			}
		}

		public void setBacherMaxCount( int count )
		{
			if(batcher != null)batcher.setMaxInstanceCount(count);
		}

        public static bool lightParseDef(string defpath)
        {
            bool hasAnimation = false;
            if (Common.Util.file.exists(defpath))
            {
                var reader = Common.Util.file.getStreamReader(defpath);
                DEFREADMODE mode = DEFREADMODE.kNONE;

                while (!reader.EndOfStream)
                {
                    string line = reader.ReadLine();
                    string[] words = line.Split(new char[] { ' ' });

                    if (words.Length == 0) continue;
                    if (words[0] == "anim")
                    {
                        mode = DEFREADMODE.kANIM;
                    }
                    else if (words[0] == "mtl" && words.Length >= 2)
                    {
                        mode = DEFREADMODE.kMTL;
                    }
                    else if (words[0] == "preview")
                    {
                        mode = DEFREADMODE.kPREVIEW;
                    }
                    else
                    {
                        switch (mode)
                        {
                            case DEFREADMODE.kANIM:
                                Func<int, string> getWord = null;
                                getWord = (int index) =>
                                {
                                    if (words.Length <= index)
                                    {
                                        switch (index)
                                        {
                                            case 1:
                                                return "0";
                                            case 2:
                                                return getWord(1);
                                            case 3:
                                            default:
                                                return "";
                                        }
                                    }
                                    return words[index];
                                };
                                if (words.Length >= 2)
                                {
                                    hasAnimation = true;
                                }
                                break;

                            case DEFREADMODE.kMTL:
                                //必要ならマテリアル（シェーダー、ブレンドなど）設定
                                if (words[0] == "blend" && words.Length >= 2)
                                {
                                }
                                else if (words[0] == "shader" && words.Length >= 2)
                                {
                                }
                                else if (words[0] == "depthwrite" && words.Length >= 2)
                                {
                                }
                                else if (words[0] == "uscrollspeed" && words.Length >= 2)
                                {
                                    hasAnimation = true;
                                }
                                else if (words[0] == "vscrollspeed" && words.Length >= 2)
                                {
                                    hasAnimation = true;
                                }
                                else if (words[0] == "texstopanim" && words.Length >= 5)
                                {
                                    hasAnimation = true;
                                }
                                break;

                            case DEFREADMODE.kPREVIEW:
                                if (words[0] == "angle" && words.Length >= 4)
                                {
                                }
                                else if (words[0] == "offset" && words.Length >= 4)
                                {
                                }
                                break;
                            case DEFREADMODE.kNONE:
                                //無効な定義
                                break;
                        }
                    }
                }

                reader.Close();
            }
            else
            {
                // 特になにもしない
            }

            return hasAnimation;
        }
	}

}
