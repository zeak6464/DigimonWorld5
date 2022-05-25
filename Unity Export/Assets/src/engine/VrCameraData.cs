
#if ENABLE_VR

using System;
using System.Diagnostics;
using Microsoft.Xna.Framework;
using Yukar.Common.Rom;

namespace Yukar.Engine
{
	//======================================================================================================================
	/**
	 *	VRカメラデータクラス
	 */
	public class VrCameraData
	{
		//++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
		//	Define
		//++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
		//------------------------------------------------------------------------------
		/**
		 *	パラメータタイプ
		 */
		public enum ParamType
		{
			Unknown = -1,	// 不明
			RotateY,		// Ｙ軸回転
			Distance,		// 距離
			Height,			// 高さ
			Num				// 定義数
		}


		//++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
		//	Class
		//++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
		//------------------------------------------------------------------------------
		/**
		 *	パラメータデータクラス
		 */
		private class ParamData
		{
			public int		m_Index			= 0;		// インデックス
			public int		m_IndexNum		= 0;		// 最大インデックス
			public bool		m_bLoop			= false;	// インデックスのループ処理を行うか

			public float	m_ValueOffset	= 0.0f;		// 数値オフセット
			public float	m_ValueInterval	= 0.0f;		// 数値変更間隔
		}


		//++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
		//	Variable
		//++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
		private ParamData[]				m_ParamData					= null;								// パラメータデータ
		
		private SharpKmyMath.Vector3	m_SaveHmdPos				= new SharpKmyMath.Vector3(0,0,0);	// HMD座標
		//private float					m_SaveHmdRotateY			= 0.0f;								// HMDＹ軸回転値

		private SharpKmyMath.Vector3	m_OutOffsetPos				= new SharpKmyMath.Vector3(0,0,0);	// 外部設定用オフセット座標
		//private float					m_OutOffsetRotateY			= 0.0f;								// 外部設定用オフセットＹ軸回転値

		private SharpKmyMath.Vector3	m_InOffsetPos				= new SharpKmyMath.Vector3(0,0,0);	// 内部設定用オフセット座標
		private float					m_InOffsetRotateY			= 0.0f;								// 内部設定用オフセットＹ軸回転値

		private SharpKmyMath.Vector3	m_CombinedOffsetPos			= new SharpKmyMath.Vector3(0,0,0);	// 統合済みオフセット座標
		private float					m_CombinedOffsetRotateY		= 0.0f;								// 統合済みオフセットＹ軸回転値
		private SharpKmyMath.Matrix4	m_CombinedOffsetRotateMatrix= SharpKmyMath.Matrix4.identity();	// 統合済みオフセット回転行列

		private float					m_HakoniwaScale				= 1.0f;								// 箱庭視点時のオブジェクトスケール

		public SharpKmyMath.Vector3		m_CameraPos;													// 一時保存用カメラ座標
		public SharpKmyMath.Vector3		m_UpVec;														// 一時保存用上ベクトル
		private Map.CameraControlMode	m_CameraCtrlMode			= Map.CameraControlMode.UNKNOWN;	// 一時保存用カメラ制御モード


		//++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
		//	Method (public)
		//++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
		//------------------------------------------------------------------------------
		/**
		 *	コンストラクタ
		 */
		public VrCameraData()
		{
			m_ParamData = new ParamData[ (int)ParamType.Num ];
			for( int n=0; n<m_ParamData.Length; n++ ) {
				m_ParamData[n] = new ParamData();
			}
		}

		//------------------------------------------------------------------------------
		/**
		 *	パラメータをセットアップ
		 */
		public void SetupParam( ParamType type, int index, int indexNum, bool bLoop, float offset=0.0f, float interval=0.0f )
		{
			Debug.Assert( type > ParamType.Unknown && type < ParamType.Num );

			ParamData data = m_ParamData[ (int)type ];

			data.m_Index		 = index;
			data.m_IndexNum		 = indexNum;
			data.m_bLoop		 = bLoop;

			data.m_ValueOffset	 = offset;
			data.m_ValueInterval = interval;
		}

		//------------------------------------------------------------------------------
		/**
		 *	箱庭視点時のスケールを設定
		 */
		public void SetHakoniwaScale( float scale )
		{
			m_HakoniwaScale = scale;
		}

		//------------------------------------------------------------------------------
		/**
		 *	箱庭視点時のスケールを取得
		 */
		public float GetHakoniwaScale()
		{
			return m_HakoniwaScale;
		}

		//------------------------------------------------------------------------------
		/**
		 *	外部設定用オフセット座標を設定
		 */
		public void SetOutOffsetPos( float x, float y, float z )
		{
			m_OutOffsetPos.x = x;
			m_OutOffsetPos.y = y;
			m_OutOffsetPos.z = z;
		}

		//------------------------------------------------------------------------------
		/**
		 *	外部設定用オフセット座標を取得
		 */
		public SharpKmyMath.Vector3 GetOutOffsetPos()
		{
			return m_OutOffsetPos;
		}

		//------------------------------------------------------------------------------
		/**
		 *	外部設定用オフセットＹ軸回転を設定
		 */
        /*
		public void SetOutOffsetRotateY( float rot )
		{
			m_OutOffsetRotateY = rot;
		}
        */

		//------------------------------------------------------------------------------
		/**
		 *	統合済みオフセット座標を取得
		 */
		public SharpKmyMath.Vector3 GetCombinedOffsetPos()
		{
			return m_CombinedOffsetPos;
		}

		//------------------------------------------------------------------------------
		/**
		 *	統合済みオフセットＹ軸回転を取得
		 */
		public float GetCombinedOffsetRotateY()
		{
			return m_CombinedOffsetRotateY;
		}

		//------------------------------------------------------------------------------
		/**
		 *	統合済みオフセット回転行列を取得
		 */
		public SharpKmyMath.Matrix4 GetCombinedOffsetRotateMatrix()
		{
			return m_CombinedOffsetRotateMatrix;
		}

		//------------------------------------------------------------------------------
		/**
		 *	Ｙ軸回転値を変更
		 */
		public void ChangeRotateY( int diff )
		{
			// 箱庭視点以外は何もしない
			if( m_CameraCtrlMode != Map.CameraControlMode.NORMAL ) {
				return;
			}

			m_ParamData[ (int)ParamType.RotateY ].m_Index += diff;

			// 情報更新
			UpdateInfo( Map.CameraControlMode.UNKNOWN );
		}

		//------------------------------------------------------------------------------
		/**
		 *	距離を変更
		 */
		public void ChangeDistance( int diff )
		{
			// 箱庭視点以外は何もしない
			if( m_CameraCtrlMode != Map.CameraControlMode.NORMAL ) {
				return;
			}

			m_ParamData[ (int)ParamType.Distance ].m_Index += diff;

			// 情報更新
			UpdateInfo( Map.CameraControlMode.UNKNOWN );
		}

		//------------------------------------------------------------------------------
		/**
		 *	高さを変更
		 */
		public void ChangeHeight( int diff )
		{
			// 箱庭視点以外は何もしない
			if( m_CameraCtrlMode != Map.CameraControlMode.NORMAL ) {
				return;
			}

			m_ParamData[ (int)ParamType.Height ].m_Index += diff;

			// 情報更新
			UpdateInfo( Map.CameraControlMode.UNKNOWN );
		}

		//------------------------------------------------------------------------------
		/**
		 *	各種情報更新
		 */
		public void UpdateInfo( Map.CameraControlMode cameraCtrlMode )
		{
			// 一時保存用カメラ制御モードを更新
			if( cameraCtrlMode != Map.CameraControlMode.UNKNOWN ) {
				m_CameraCtrlMode = cameraCtrlMode;
			}

			// カメラ制御モードが設定されていない
			Debug.Assert( m_CameraCtrlMode != Map.CameraControlMode.UNKNOWN );

			Debug.Assert( m_ParamData[ (int)ParamType.RotateY ].m_IndexNum > 0 );
			Debug.Assert( m_ParamData[ (int)ParamType.Distance ].m_IndexNum > 0 );
			Debug.Assert( m_ParamData[ (int)ParamType.Height ].m_IndexNum > 0 );

			// クリア
			m_InOffsetPos.x		= 0.0f;
			m_InOffsetPos.y		= 0.0f;
			m_InOffsetPos.z		= 0.0f;
			m_InOffsetRotateY	= 0.0f;

			// カメラ制御モード別処理
			switch( m_CameraCtrlMode )
			{
			case Map.CameraControlMode.NORMAL:
				{
					// 各インデックスの調整
					AdjustIndex( ParamType.RotateY );
					AdjustIndex( ParamType.Distance );
					AdjustIndex( ParamType.Height );

					float rotateY	= GetParam( ParamType.RotateY );
					float distance	= GetParam( ParamType.Distance );
					float height	= GetParam( ParamType.Height );

					float shiftX = distance * (float)Math.Sin( rotateY );
					float shiftY = distance * (float)Math.Cos( rotateY );

					m_InOffsetPos.x = shiftX + m_OutOffsetPos.x;// - (m_SaveHmdPos.x * m_HakoniwaScale);
					m_InOffsetPos.y = height + m_OutOffsetPos.y;
					m_InOffsetPos.z = shiftY + m_OutOffsetPos.z;// - (m_SaveHmdPos.z * m_HakoniwaScale);

					m_InOffsetRotateY = rotateY;
				}
				break;

			case Map.CameraControlMode.VIEW:
				m_InOffsetPos = -m_SaveHmdPos;
				break;

#if ENABLE_GHOST_MOVE
			case Map.CameraControlMode.GHOST:
				m_InOffsetPos		= -m_SaveHmdPos;
				m_InOffsetRotateY	= m_OutOffsetRotateY + MathHelper.ToRadians(180);
				break;
#endif  // #if ENABLE_GHOST_MOVE

			default:
				Debug.Assert( false, "無効なカメラモード" );
				break;
			}

			// 各統合済みオフセット値を算出
			m_CombinedOffsetPos		= m_InOffsetPos;
			m_CombinedOffsetRotateY	= m_InOffsetRotateY;

			// 統合済みオフセット回転行列を算出
			m_CombinedOffsetRotateMatrix = SharpKmyMath.Matrix4.rotateY( m_CombinedOffsetRotateY );
		}

		//------------------------------------------------------------------------------
		/**
		 *	キャリブレーション
		 */
		public void Calibration()
		{
			// 箱庭視点時はキャリブレーションを行わない
			if( m_CameraCtrlMode == Map.CameraControlMode.NORMAL ) {
				return;
			}

			// HMD座標を保存
			SharpKmyMath.Vector3 hmdPos = SharpKmyVr.Func.GetHmdPosePos();
			m_SaveHmdPos.x = hmdPos.x;
			m_SaveHmdPos.y = hmdPos.y;
			m_SaveHmdPos.z = hmdPos.z;

			// HMDＹ軸回転値を保存
			//m_SaveHmdRotateY = SharpKmyVr.Func.GetHmdRotateY();

			// 情報更新
			UpdateInfo( Common.Rom.Map.CameraControlMode.UNKNOWN );
		}


		//++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
		//	Method (private)
		//++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
		//------------------------------------------------------------------------------
		/**
		 *	インデックスの調整
		 */
		private void AdjustIndex( ParamType type )
		{
			ParamData data = m_ParamData[ (int)type ];

			if( data.m_bLoop )
			{
				if( data.m_Index < 0 ) {
					data.m_Index = data.m_IndexNum - 1;
				} else if( data.m_Index >= data.m_IndexNum ) {
					data.m_Index = 0;
				}
			}
			else
			{
				data.m_Index = MathHelper.Clamp( data.m_Index, 0, data.m_IndexNum-1 );
			}
		}

		//------------------------------------------------------------------------------
		/**
		 *	パラメータを取得
		 */
		private float GetParam( ParamType type )
		{
			float ret = 0.0f;
			ParamData data = m_ParamData[ (int)type ];

			// Ｙ軸回転のみ特殊処理
			if( type == ParamType.RotateY )
			{
				float interval = 360 / data.m_IndexNum;
				ret = MathHelper.ToRadians( interval * data.m_Index );
			}
			else
			{
				ret = data.m_ValueOffset + (data.m_ValueInterval * data.m_Index);
			}

			return ret;
		}
	}
}

#endif	// #if ENABLE_VR


