
#if ENABLE_VR

using System.Diagnostics;

namespace Yukar.Engine
{
	//======================================================================================================================
	/**
	 *	VR簡易描画用Drawableクラス
	 */
	public class VrSimpleDrawable : SharpKmyGfx.Drawable
	{
		//++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
		//	Method (public)
		//++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
		//------------------------------------------------------------------------------
		/**
		 *	描画
		 */
		public override void draw( SharpKmyGfx.Render scn )
		{
			if( SharpKmyGfx.Render.isSameScene( scn, SharpKmyGfx.Render.getRenderL() ) ||
				SharpKmyGfx.Render.isSameScene( scn, SharpKmyGfx.Render.getRenderR() ) )
			{
				SharpKmyMath.Vector3 posCam = new SharpKmyMath.Vector3(0,0,0);
				VrDrawer.DrawVr2dPolygon( scn, posCam );
			}
		}
	}


	//======================================================================================================================
	/**
	 *	VR用描画関連処理クラス
	 */
	public class VrDrawer
	{
		//++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
		//	Define
		//++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
		public const float	VR_2D_POLYGON_DISTANCE		= 3.5f;		// 2D表示用ポリゴン距離		※とりあえず固定。最終的にはクリエイター/ユーザーが変更できるようにしたほうが良さそう。
		public const int	VR_2D_POLYGON_LAYER			= 1022;		// 2D表示用ポリゴン用レイヤー値

		//------------------------------------------------------------------------------
		/**
		 *	描画データID
		 */
		private enum DrawDataId
		{
			Unknown = -1,		// 不明

			Poly2D,				// 2Dポリゴン

			PolyFillRect1_U,	// 塗りつぶし矩形ポリゴン１・上
			PolyFillRect1_D,	// 塗りつぶし矩形ポリゴン１・下
			PolyFillRect1_L,	// 塗りつぶし矩形ポリゴン１・左
			PolyFillRect1_R,	// 塗りつぶし矩形ポリゴン１・右

			PolyFillRect2_U,	// 塗りつぶし矩形ポリゴン２・上
			PolyFillRect2_D,	// 塗りつぶし矩形ポリゴン２・下
			PolyFillRect2_L,	// 塗りつぶし矩形ポリゴン２・左
			PolyFillRect2_R,	// 塗りつぶし矩形ポリゴン２・右

			Num					// 定義数
		}

		//------------------------------------------------------------------------------
		/**
		 *	描画データクラス
		 */
		private class DrawData
		{
			public SharpKmyGfx.DrawInfo						m_DrawInfo;		// 描画情報
			public SharpKmyGfx.VertexPositionTextureColor[]	m_Vertices;		// 頂点データ
		}


		//++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
		//	Variable
		//++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
		static private VrSimpleDrawable		m_VrSimpleDrawable;		// VR簡易描画用Drawable
		
		static private SharpKmyGfx.Shader	m_ShaderTex		= null;		// シェーダ：テクスチャあり
		static private SharpKmyGfx.Shader	m_ShaderNoTex	= null;		// シェーダ：テクスチャなし
		static private DrawData[]			m_DrawData		= null;		// 描画データ

		static private bool					m_bInit			= false;	// 初期化済みフラグ


		//++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
		//	Method (public)
		//++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
		//------------------------------------------------------------------------------
		/**
		 *	初期化
		 */
		public static void Init()
		{
			Debug.Assert( !m_bInit );

			m_VrSimpleDrawable = new VrSimpleDrawable();
			
			m_ShaderTex		= SharpKmyGfx.Shader.load("legacy_tex");
			m_ShaderNoTex	= SharpKmyGfx.Shader.load("legacy_notex");

			m_DrawData = new DrawData[ (int)DrawDataId.Num ];
			for( int n=0; n<m_DrawData.Length; n++ ) {
				m_DrawData[n] = new DrawData();
				m_DrawData[n].m_DrawInfo = new SharpKmyGfx.DrawInfo();
				m_DrawData[n].m_Vertices = new SharpKmyGfx.VertexPositionTextureColor[6];
			}

			m_bInit = true;
		}

		//------------------------------------------------------------------------------
		/**
		 *	VR描画
		 */
		public static void DrawVr( SharpKmyGfx.Drawable drawable, VrCameraData vcd )
		{
			if( !SharpKmyVr.Func.IsReady() ) {
				return;
			}

			SharpKmyMath.Vector3 tmpDir;
			SharpKmyMath.Vector3 tmpEyeDir;
			SharpKmyMath.Vector3 posEye;
			SharpKmyMath.Vector3 posTarget;
			SharpKmyMath.Vector3 vecCamPos	= (vcd == null) ? new SharpKmyMath.Vector3(0,0,0) : vcd.m_CameraPos;
			SharpKmyMath.Vector3 vecUp		= (vcd == null) ? new SharpKmyMath.Vector3(0,1,0) : vcd.m_UpVec;
			SharpKmyMath.Matrix4 mtxRot		= (vcd == null) ? SharpKmyMath.Matrix4.identity() : vcd.GetCombinedOffsetRotateMatrix();
			SharpKmyMath.Matrix4 mtxProj;
			SharpKmyMath.Matrix4 mtxView;

			// 上方ベクトル調整
			if( vcd == null )
			{
				SharpKmyMath.Matrix4 mtxTmp = SharpKmyVr.Func.GetHmdPoseRotateMatrix() * SharpKmyMath.Matrix4.translate(0,1,0);
				vecUp.x = mtxTmp.m30;
				vecUp.y = mtxTmp.m31;
				vecUp.z = mtxTmp.m32;
				vecUp = SharpKmyMath.Vector3.normalize( vecUp );
			}

			for( int n=0; n<2; n++ )
			{
				SharpKmyVr.EyeType eyeType;
				SharpKmyGfx.Render render;

				if( n == 0 ) {
					eyeType	= SharpKmyVr.EyeType.Left;
					render  = SharpKmyGfx.Render.getRenderL();
				} else {
					eyeType	= SharpKmyVr.EyeType.Right;
					render  = SharpKmyGfx.Render.getRenderR();
				}

                if (render == null) continue;
#if true
				tmpDir		= SharpKmyVr.Func.GetHmdPoseDirection();
				tmpEyeDir	= (mtxRot * SharpKmyMath.Matrix4.translate( tmpDir.x, tmpDir.y, tmpDir.z )).translation();

				posEye		= vecCamPos;
				posTarget	= posEye + tmpEyeDir;

				mtxProj = SharpKmyVr.Func.GetProjectionMatrix( eyeType );
				mtxView = SharpKmyMath.Matrix4.lookat( posEye, posTarget, vecUp );
				mtxView = SharpKmyMath.Matrix4.inverse( mtxView );
#else
				mtxProj	= SharpKmyVr.Func.GetProjectionMatrix( eyeType );
				mtxView	= SharpKmyVr.Func.GetViewMatrix( eyeType, vecUp, vecPos, mtxRot );
#endif

				render.setViewMatrix( mtxProj, mtxView );
				render.addDrawable( drawable );
			}
		}

		//------------------------------------------------------------------------------
		/**
		 *	VR描画
		 */
		public static void DrawVr( SharpKmyGfx.Drawable drawable, VrCameraData vcd, SharpKmyMath.Matrix4 mtxProj, SharpKmyMath.Matrix4 mtxView )
		{
			if( !SharpKmyVr.Func.IsReady() ) {
				return;
			}

#if !VR_SIDE_BY_SIDE
			// 通常描画
			SharpKmyGfx.Render.getDefaultRender().setViewMatrix( mtxProj, mtxView );
			SharpKmyGfx.Render.getDefaultRender().addDrawable( drawable );
#endif  // #if !VR_SIDE_BY_SIDE

			// VR用
			DrawVr( drawable, vcd );
		}

		//------------------------------------------------------------------------------
		/**
		 *	VR描画（簡易版）
		 *
		 *	@note	元々3D描画を行っていなかったシーンでの使用を想定
		 */
		public static void DrawSimple( float aspect )
		{
			if( !SharpKmyVr.Func.IsReady() ) {
				return;
			}

			// 描画
			DrawVr( m_VrSimpleDrawable, null );
		}

		//------------------------------------------------------------------------------
		/**
		 *	VR用2Dポリゴン描画
		 */
		public static void DrawVr2dPolygon( SharpKmyGfx.Render scn, SharpKmyMath.Vector3 posCam, VrCameraData vcd=null )
		{
            var texture = SharpKmyGfx.Render.getRender2D().getColorTexture();
            if (texture == null) return;

            DrawVrPolygon(
				scn,
				posCam,
				SharpKmyGfx.Color.White,
				0,
				vcd,
                texture,
				DrawDataId.Poly2D,
				VR_2D_POLYGON_DISTANCE
				);
		}

		//------------------------------------------------------------------------------
		/**
		 *	VR用2Dポリゴン描画
		 */
		public static void DrawVr2dPolygon( SharpKmyGfx.Render scn, SharpKmyMath.Vector3 posCam, SharpKmyGfx.Color col, bool bMain, VrCameraData vcd=null )
		{
			DrawVrPolygon(
				scn,
				posCam,
				SharpKmyGfx.Color.White,
				0,
				vcd,
				SharpKmyGfx.Render.getRender2D().getColorTexture(),
				DrawDataId.Poly2D,
				VR_2D_POLYGON_DISTANCE
				);
			
			DrawVrPolygon( scn, posCam, col, 0, vcd, null, (bMain) ? DrawDataId.PolyFillRect1_U : DrawDataId.PolyFillRect2_U, VR_2D_POLYGON_DISTANCE );
			DrawVrPolygon( scn, posCam, col, 0, vcd, null, (bMain) ? DrawDataId.PolyFillRect1_D : DrawDataId.PolyFillRect2_D, VR_2D_POLYGON_DISTANCE );
			DrawVrPolygon( scn, posCam, col, 0, vcd, null, (bMain) ? DrawDataId.PolyFillRect1_L : DrawDataId.PolyFillRect2_L, VR_2D_POLYGON_DISTANCE );
			DrawVrPolygon( scn, posCam, col, 0, vcd, null, (bMain) ? DrawDataId.PolyFillRect1_R : DrawDataId.PolyFillRect2_R, VR_2D_POLYGON_DISTANCE );
		}

		//------------------------------------------------------------------------------
		/**
		 *	VR用2Dポリゴン描画
		 */
		public static void DrawVr2dPolygon( SharpKmyGfx.Render scn, SharpKmyMath.Vector3 posCam, SharpKmyGfx.Color col, bool bMain, int layerOffset, VrCameraData vcd=null )
		{
			DrawVrPolygon(
				scn,
				posCam,
				SharpKmyGfx.Color.White,
				layerOffset,
				vcd,
				SharpKmyGfx.Render.getRender2D().getColorTexture(),
				DrawDataId.Poly2D,
				VR_2D_POLYGON_DISTANCE
				);

			DrawVrPolygon( scn, posCam, col, layerOffset, vcd, null, (bMain) ? DrawDataId.PolyFillRect1_U : DrawDataId.PolyFillRect2_U, VR_2D_POLYGON_DISTANCE );
			DrawVrPolygon( scn, posCam, col, layerOffset, vcd, null, (bMain) ? DrawDataId.PolyFillRect1_D : DrawDataId.PolyFillRect2_D, VR_2D_POLYGON_DISTANCE );
			DrawVrPolygon( scn, posCam, col, layerOffset, vcd, null, (bMain) ? DrawDataId.PolyFillRect1_L : DrawDataId.PolyFillRect2_L, VR_2D_POLYGON_DISTANCE );
			DrawVrPolygon( scn, posCam, col, layerOffset, vcd, null, (bMain) ? DrawDataId.PolyFillRect1_R : DrawDataId.PolyFillRect2_R, VR_2D_POLYGON_DISTANCE );
		}

		//------------------------------------------------------------------------------
		/**
		 *	VR用塗りつぶし矩形ポリゴン描画
		 */
		public static void DrawVrFillRectPolygon( SharpKmyGfx.Render scn, SharpKmyMath.Vector3 posCam, SharpKmyGfx.Color col, bool bMain, int layerOffset, VrCameraData vcd=null )
		{
			DrawVrPolygon( scn, posCam, col, layerOffset, vcd, null, (bMain) ? DrawDataId.PolyFillRect1_U : DrawDataId.PolyFillRect2_U, VR_2D_POLYGON_DISTANCE );
			DrawVrPolygon( scn, posCam, col, layerOffset, vcd, null, (bMain) ? DrawDataId.PolyFillRect1_D : DrawDataId.PolyFillRect2_D, VR_2D_POLYGON_DISTANCE );
			DrawVrPolygon( scn, posCam, col, layerOffset, vcd, null, (bMain) ? DrawDataId.PolyFillRect1_L : DrawDataId.PolyFillRect2_L, VR_2D_POLYGON_DISTANCE );
			DrawVrPolygon( scn, posCam, col, layerOffset, vcd, null, (bMain) ? DrawDataId.PolyFillRect1_R : DrawDataId.PolyFillRect2_R, VR_2D_POLYGON_DISTANCE );
		}

		//------------------------------------------------------------------------------
		/**
		 *	VR用ポリゴン描画
		 */
		private static void DrawVrPolygon( SharpKmyGfx.Render scn, SharpKmyMath.Vector3 posCam, SharpKmyGfx.Color col, int layerOffset, VrCameraData vcd, SharpKmyGfx.Texture tex, DrawDataId ddId, float distance )
		{
#if ENABLE_VR_UNITY
			return;
#endif
			if( col.a <= 0.0f ) {
				return;
			}

			DrawData dd = m_DrawData[ (int)ddId ];

			SharpKmyMath.Matrix4 mtxTmp;
			SharpKmyMath.Matrix4 mtxRot = (vcd != null) ? vcd.GetCombinedOffsetRotateMatrix() : SharpKmyMath.Matrix4.identity();

			if( tex != null ) {
				dd.m_DrawInfo.setShader( m_ShaderTex );
				dd.m_DrawInfo.setTexture( 0, tex );
			} else {
				dd.m_DrawInfo.setShader( m_ShaderNoTex );
			}
			dd.m_DrawInfo.setLayer( VR_2D_POLYGON_LAYER + layerOffset );

			// 視線ベクトル
			SharpKmyMath.Vector3 vecDir = SharpKmyVr.Func.GetHmdPoseDirection();
			vecDir = (mtxRot * SharpKmyMath.Matrix4.translate( vecDir.x, vecDir.y, vecDir.z )).translation();

			// 上ベクトル
			mtxTmp = mtxRot * SharpKmyVr.Func.GetHmdPoseRotateMatrix() * SharpKmyMath.Matrix4.translate(0,1,0);
			SharpKmyMath.Vector3 vecUp  = SharpKmyMath.Vector3.normalize( mtxTmp.translation() );
			SharpKmyMath.Vector3 vecUpS = vecUp;

			// 横ベクトル
			SharpKmyMath.Vector3 vecSide = SharpKmyMath.Vector3.normalize( SharpKmyMath.Vector3.crossProduct( vecDir, vecUp ) );
			SharpKmyMath.Vector3 vecSideS = vecSide * ((float)Graphics.ViewportWidth / (float)Graphics.ViewportHeight);

			// 頂点設定
			{
				const float _ScaleW = 2.0f;
				const float _ScaleH = 5.0f;
				bool bDraw = true;

				SharpKmyMath.Vector3 _BasePos	= posCam + (vecDir * distance) - (vecUp * 0.5f);// - (vecDir * (float)layerOffset *0.2f);
				SharpKmyMath.Vector3 _BasePosLU	= _BasePos - vecSideS + vecUpS;
				SharpKmyMath.Vector3 _BasePosLD	= _BasePos - vecSideS - vecUpS;
				SharpKmyMath.Vector3 _BasePosRU	= _BasePos + vecSideS + vecUpS;
				SharpKmyMath.Vector3 _BasePosRD	= _BasePos + vecSideS - vecUpS;
				SharpKmyMath.Vector3 posLU		= _BasePosLU;
				SharpKmyMath.Vector3 posLD		= _BasePosLD;
				SharpKmyMath.Vector3 posRU		= _BasePosRU;
				SharpKmyMath.Vector3 posRD		= _BasePosRD;

				switch( ddId )
				{
				case DrawDataId.Poly2D:
					posLU = _BasePosLU;
					posLD = _BasePosLD;
					posRU = _BasePosRU;
					posRD = _BasePosRD;
					break;

				case DrawDataId.PolyFillRect1_U:
				case DrawDataId.PolyFillRect2_U:
					posLU = _BasePosLU + (-vecSideS * _ScaleW) + (vecUpS * _ScaleH);
					posLD = _BasePosLU + (-vecSideS * _ScaleW);
					posRU = _BasePosRU + ( vecSideS * _ScaleW) + (vecUpS * _ScaleH);
					posRD = _BasePosRU + ( vecSideS * _ScaleW);
					break;

				case DrawDataId.PolyFillRect1_D:
				case DrawDataId.PolyFillRect2_D:
					posLU = _BasePosLD + (-vecSideS * _ScaleW) - (vecUpS * _ScaleH);
					posLD = _BasePosLD + (-vecSideS * _ScaleW);
					posRU = _BasePosRD + ( vecSideS * _ScaleW) - (vecUpS * _ScaleH);
					posRD = _BasePosRD + ( vecSideS * _ScaleW);
					break;

				case DrawDataId.PolyFillRect1_L:
				case DrawDataId.PolyFillRect2_L:
					posLU = _BasePosLU + (-vecSideS * _ScaleW);
					posLD = _BasePosLD + (-vecSideS * _ScaleW);
					posRU = _BasePosLU;
					posRD = _BasePosLD;
					break;

				case DrawDataId.PolyFillRect1_R:
				case DrawDataId.PolyFillRect2_R:
					posLU = _BasePosRU;
					posLD = _BasePosRD;
					posRU = _BasePosRU + ( vecSideS * _ScaleW);
					posRD = _BasePosRD + ( vecSideS * _ScaleW);
					break;

				default:
					bDraw = false;
					break;
				}

				if( bDraw )
				{
					SharpKmyMath.Vector3[] positions = new SharpKmyMath.Vector3[6]{
						posLU,
						posLD,
						posRU,

						posLD,
						posRU,
						posRD,
					};

					SharpKmyMath.Vector2[] tc = new SharpKmyMath.Vector2[6]{
						new SharpKmyMath.Vector2(0,0),
						new SharpKmyMath.Vector2(0,1),
						new SharpKmyMath.Vector2(1,0),

						new SharpKmyMath.Vector2(0,1),
						new SharpKmyMath.Vector2(1,0),
						new SharpKmyMath.Vector2(1,1)
					};

					// 頂点設定
					for( int n=0; n<6; n++ )
					{
						dd.m_Vertices[n].pos	= positions[n];
						dd.m_Vertices[n].tc		= new SharpKmyMath.Vector2(tc[n].x, 1.0f-tc[n].y);
						dd.m_Vertices[n].color	= col;
					}

					// 情報設定
					dd.m_DrawInfo.setVolatileVertex( dd.m_Vertices );
					dd.m_DrawInfo.setBlend(SharpKmyGfx.BLENDTYPE.kPREMULTIPLIED );
					dd.m_DrawInfo.setDepthFunc( SharpKmyGfx.FUNCTYPE.kALWAYS );
					dd.m_DrawInfo.setIndexCount( 6 );

					// 描画登録
					scn.draw( dd.m_DrawInfo );
				}
			}
		}
	}
}

#endif  // #if ENABLE_VR


