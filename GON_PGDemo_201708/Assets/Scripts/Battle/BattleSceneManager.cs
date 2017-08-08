//*****************************************************
//  BattleSceneManager.cs
//  作成者：Vazquez Gonzalo (xxxgonxxx@gmail.com)
//  更新：2017.08.08 (第一稿)
//*****************************************************



using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BattleSceneManager : MonoBehaviour
{

    #region パラメータ


    //サウンド
    public GameObject au_sound_1038;
    public GameObject au_sound_1037;
    public GameObject au_sound_1007;
    public GameObject au_sound_punch_01;
    public GameObject au_sound_punch_02;


    //システム系の定義
    private bool b_pause = false;               //ポーズのフラグ
    private bool b_focus = true;                //ゲーム再開（逆ポーズ）のフラグ
    private int i_screenRes_pixel_height = 900;   //画面の解像度（縦。横は自動で計算される）


    //チームのユニット
    //デバッグ用のために全プレイヤーユニットのクラスをオブジェクトで用意している
    private PlayerUnit pu_team_unit01 = null;
    private PlayerUnit pu_team_unit02 = null;
    private PlayerUnit pu_team_unit03 = null;
    private PlayerUnit pu_team_unit04 = null;
    private PlayerUnit pu_team_unit05 = null;
    private PlayerUnit pu_team_unit06 = null;
    private PlayerUnit pu_team_unit07 = null;
    private PlayerUnit pu_team_unit08 = null;

    private bool updateHUD_unit_01 = false;
    private bool updateHUD_unit_02 = false;
    private bool updateHUD_team = false;

    internal GameObject go_selectedUnit = null;

    //バトルの仕組み関連
    internal bool performActionFlag = false;
    internal bool processingActionFlag = false;
    internal GameObject charIndicator;

    internal bool sceneInitialized = false;

    //開始演出


    //終了演出




    #endregion





    #region ポーズ
    public void EnablePausePlay()
    {
        if (!b_pause)
        {
            b_pause = true;
            b_focus = !b_pause;
            OnApplicationPause(b_pause);
        }
        else
        {
            b_pause = false;
            b_focus = !b_pause;
            OnApplicationFocus(b_focus);
        }

    }

    public void DisablePause()
    {

    }

    #endregion


    #region 画面

    //スクリーンのスリープモードの設定
    //説明：
    // 1. ポーズ：
    //          スリープモードを無効にする　（一旦、プロトとして、これだけで色々なバッグがなくなる：サウンドや画面等）
    //          ※備考：今回は、電池消費のために、ポーズの時だけにスリープを有効にしている
    //          TO DO: スリープモードを有効にして、ゲームはバグらないような調整を行う
    //
    //  2. ゲームの再開：
    //  スリープモードを有効にする。ユーザーの端末のデフォルト設定にリセット（電池消費のためだ）
    private void SetScreenTimeout()
    {
        //1.ポーズ
        if (b_pause)
            Screen.sleepTimeout = SleepTimeout.NeverSleep;

        //2.ゲームの再開
        if (b_focus)
            Screen.sleepTimeout = SleepTimeout.SystemSetting;
    }

    //解像度の設定
    private void SetScreenResolution()
    {
        //縦の16:9のアスペクト比の想定。タブレットは縦のLetter Boxで
        //最適化のために、解像度を下げる
        //最適化として、
        //TO DO: 端末のGPUデ判断して、解像度をあげる
        //TO DO: ユーザーは解像度を設定するような機能の追加

        //アスペクト比を基にして、画面の解像度を計算する
        float f_aspectRatio = (float)Screen.height / (float)Screen.width;
        int i_screenRes_pixel_width = (int)((float)i_screenRes_pixel_height / f_aspectRatio);

        //解像度の変更（Editorでチェック不可。端末で確認するはず）
        Screen.SetResolution(i_screenRes_pixel_width, i_screenRes_pixel_height, true);

        Debug.Log("解像度の設定は完了：　" + Screen.currentResolution);
    }

    #endregion


    #region ポインター
    //ポインターのモード
    internal enum PointerMode
    {
        UnitSelectionMode,                //ユニットを選択するモード
        UnitMoveMode,                     //ユニットを移動するモード
        NUM
    }

    internal PointerMode pointerMode;
    private Vector2 pointer_unit_initCoord = new Vector2(0.0f, 0.0f);                     //ユニットの移動の機能にて、ユニットのもとの座標を格納するvector。x = 行 y = カラム
    private Vector2 pointer_unit_initCoord_beforeOverride = new Vector2(0.0f, 0.0f);      //ユニットの移動の機能にて、ユニットのもとの座標を格納するvector。x = 行 y = カラム。　ユニットTypeDの専用の処理のため。
    private Vector2 pointer_unit_finalCoord = new Vector2(0.0f, 0.0f);                    //ユニットの移動の機能にて、ユニットの対象の座標を格納するvector。x = 行 y = カラム
    private Vector2 pointer_unit_initCoord_beforeOverride_TypeD2 = new Vector2(0.0f, 0.0f); //ユニットの移動の機能にて、ユニットのもとの座標を格納するvector。x = 行 y = カラム。　ユニットTypeDの斜めの処理のための専用パラメータ。
    private bool pointer_unit_TypeD2_x2_Aux = false;                                        //TypeDの斜めの処理のためのフラグ
    private bool pointer_unit_TypeD2_x3_Aux = false;                                        //TypeDの斜めの処理のためのフラグ
    private Vector2 pointer_unit_TypeD2_x2_Aux_Coord = new Vector2(0, 0);                    //TypeDのユニットの位置の切り替えのパラメータ
    private Vector2 pointer_unit_TypeD2_x3_Aux_Coord = new Vector2(0, 0);                    //TypeDのユニットの位置の切り替えのパラメータ


    //サウンド用のフラグ
    private Vector2 unit_Move_LastPosition = new Vector2(0, 0);


    //ポインターのモードをセットする：ユニットを選択するモード
    internal void SetPointerModeToSelectionMode()
    {
        pointerMode = PointerMode.UnitSelectionMode;
    }

    //ポインターのモードをセットする：ユニットを移動するモード
    internal void SetPointerModeToMoveMode()
    {
        pointerMode = PointerMode.UnitMoveMode;
    }

    //ポインターが選択したユニットの座標をクラスにセットする
    internal void SetPointerUnitOriginalCoord(Vector2 coord, bool permission)
    {
        pointer_unit_initCoord = coord;
        //移動の処理（オブジェクトの階層の変更）を許す
        ChangeUnitCoord_Flag(permission);
    }

    //ポインターが検討しているユニットの座標をクラスにセットする
    // ui3DEventHandlerのOnPointerEnterとOnPointerExit(Hoverの認識)で呼び出されている
    internal void SetPointerUnitCheckingCoord(Vector2 coord, bool permission)
    {
        //移動の対象の座標をこのクラスで取得
        pointer_unit_finalCoord = coord;
        //移動のChecking(検討中)の処理をONにする
        ChangeUnitCheckCoord_Flag(permission);
    }

    //移動の処理を終わらせる
    //      ui3DEventhandler（UIに追加するコンポーネント）のOnPointerUpにて、このBattleSceneManagerのPointerModeはUnitMoveModeであれば、取得する。
    internal void EndUnitMoveMode(bool permission)
    {
        //決定担った移動の処理。（移動のCheckingの処理を止める）
        ChangeUnitCheckCoord_Flag(!permission);
        ChangeUnitCoord_Flag(!permission);
        SetChangeUnitCoord_Process_UnitGrabbed(false);              //ユニットの取得のフラグをリセットする
        SetPointerUnitOriginalCoord(new Vector2(0, 0), false);       //ユニット移動用の座標をリセット
        SetPointerUnitCheckingCoord(new Vector2(0, 0), false);      //ユニット移動用の座標をリセット
        pointer_unit_initCoord_beforeOverride = new Vector2(0, 0);      //ユニット移動用の座標をリセット
        nanameInsideSameTypeDMass = false;                          //斜めの処理：TypeDのマスの中でTypeSの斜めの動きの認識で利用されるフラグのリセット
        unitInDestinyMassCheck_Coord = new Vector2(0, 0);           //ユニットの切り替えのためのフラグのリセット
        pointer_unit_initCoord_beforeOverride_TypeD2 = new Vector2(0.0f, 0.0f); //ユニットの移動の機能にて、ユニットのもとの座標を格納するvector。x = 行 y = カラム。　ユニットTypeDの斜めの処理のための専用パラメータ。
        pointer_unit_TypeD2_x2_Aux = false;
        pointer_unit_TypeD2_x3_Aux = false;
        pointer_unit_TypeD2_x2_Aux_Coord = new Vector2(0, 0);                    //TypeDのユニットの位置の切り替えのパラメータ
        pointer_unit_TypeD2_x3_Aux_Coord = new Vector2(0, 0);                    //TypeDのユニットの位置の切り替えのパラメータ
        unit_Move_LastPosition = new Vector2(0, 0);
    }
    #endregion



    #region ユニットのグリッドの移動関連

    #region //ユニットの移動関連のパラメータ
    private bool changeUnitCoord = false;           //グリッドでユニットの移動を行うためのフラグ (決定版）
    private bool changeUnitCoord_Checking = false;  //グリッドでユニットの移動を行うためのフラグ (検討中版）

    private bool nanameInsideSameTypeDMass = false;                     //斜めの処理：TypeDのマスの中でTypeSの斜めの動きの認識で利用されるフラグ
    private int unit_naname_move_frame_buffer_max = 0;                  //斜めの処理：カクカクの動きが見えないような待ち中のフレームの定義
    private int unit_naname_move_frame_buffer_max_2axis = 1;            //斜めの処理：カクカクの動きが見えないような待ち中のフレームのカウント
    private int unit_naname_move_frame_buffer_max_1axis = 0;            //斜めの処理：カクカクの動きが見えないような待ち中のフレームのカウント
    private int unit_naname_move_frame_buffer_count = 0;                //斜めの処理：カクカクの動きが見えないような待ち中のフレームのカウント

    private bool changeUnitCoord_Process_UnitGrabbed = false;       //グリッドでユニットを移動するときに、移動しようとしているユニットの取得を認識するフラグ
    private Transform changeUnitCoord_Process_UnitGrabbedTrans;     //グリッドでユニットを移動するときに、移動しようとしているユニットのトランスフォームをここに格納する
    private string unitTypeSD_IsD = "d";                            //ユニットタイプは大きい(d = double)
    private string unitTypeSD_IsS = "s";                            //ユニットタイプは普通(s = single)
    private bool unitGrabbed_IsTypeD = false;                       // グリッドでユニットを移動するときに、移動しようとしているユニットのSDタイプ(single or double)をもとにして、処理を行うためのフラグ

    #endregion

    //ユニットの移動のフラグの設定：ui3dEventHandlerでタップの解除がある場合、呼び出されている
    private void ChangeUnitCoord_Flag(bool permission)
    {
        changeUnitCoord = permission;
    }

    //ユニットの移動のフラグの設定：ui3dEventHandlerでタップ(タップ+Hover)をしている場合、呼び出されている
    private void ChangeUnitCheckCoord_Flag(bool permission)
    {
        changeUnitCoord_Checking = permission;
    }

    //グリッドでユニットを移動するときに、移動しようとしているユニットの取得を認識するフラグを設定する
    private void SetChangeUnitCoord_Process_UnitGrabbed(bool permission)
    {
        changeUnitCoord_Process_UnitGrabbed = permission;
    }

    private Vector2 unitInDestinyMassCheck_Coord = new Vector2(0, 0);   //ユニットの切り替えのためのフラグ
    //ユニットの移動の処理
    //引数：
    //  initcoord：ui3DEventhandler（UIに追加するコンポーネント）のOnPointerDownにて、このBattleSceneManagerのPointerModeはUnitSelectionModeであれば、取得する。
    //  finalcoord：ui3DEventhandler（UIに追加するコンポーネント）OnPointerEnterとOnPointerExit(Hoverの処理)でこのBattleSceneManagerのPointerModeはUnitMoveModeであれば、取得する。
    private void ChangeUnitCoord_Process(Vector2 initcoord, Vector2 finalcoord)
    {
        Debug.Log("ユニットのoriginal座標(選択したユニットのもとの座標)。ユニットの認識の前。" + initcoord + " 。　※注意：一回サイクルがkな量になったら、TypeDのオーバーライドがありかもしれない。");
        Debug.Log("ユニットのfinal座標（移動の対処の座標）。ユニットの認識の前。" + finalcoord + " 。オーバーライドがないから、いつもポインターから取得したデータをここで確認するのが可能。");


        //*********************************************
        //ユニットを取得してあるかどうか確認する
        //*********************************************
        //ユニットを取得してない場合(ユニットを取得してある場合、専用の処理がない)
        if (!changeUnitCoord_Process_UnitGrabbed)
        {
            //斜め処理のチェックのために、ポインターから取得した座標を格納する
            pointer_unit_initCoord_beforeOverride = initcoord;
            pointer_unit_initCoord_beforeOverride_TypeD2 = initcoord;
            bool unitIsGrabbed = false;    //ユニットの取得のフラグ
            bool isBig = false;             //大きいユニットのチェック用のフラグ


            //*********************************************
            //グリッドのマスのノードにユニットがあるかどうかという確認を行う（空のグリッドを取得できないようにする）
            //*********************************************
            //ユニットが認識されていない場合は
            if (GameObject.Find("gt_" + int.Parse(initcoord.x.ToString()) + unitTypeSD_IsS + int.Parse(initcoord.y.ToString())).transform.childCount == 0)
            {
                //大きいユニット（マスを二個利用するユニット）であろうかという確認をする
                switch (int.Parse(initcoord.y.ToString()))
                {
                    //左のカラム(カラム1)　S[x,1]
                    case 1:
                        //D[X,1]にユニットがあれば、取得する
                        if (GameObject.Find("gt_" + int.Parse(initcoord.x.ToString()) + "d1").transform.childCount > 0)
                        {
                            //ユニットのもとの座標をオーバーライドする
                            SetPointerUnitOriginalCoord(new Vector2(initcoord.x, 1), true);
                            initcoord = pointer_unit_initCoord;
                            //大きいユニットのチェック用のフラグを立てる
                            isBig = true;
                            //ユニットがあれば、認識する
                            unitIsGrabbed = true;
                        }
                        break;
                    //真ん中のカラム2　S[x,2]
                    case 2:
                        //D[X,1]にユニットがあれば、取得する
                        if (GameObject.Find("gt_" + int.Parse(initcoord.x.ToString()) + "d1").transform.childCount > 0)
                        {
                            //ユニットのもとの座標をオーバーライドする
                            SetPointerUnitOriginalCoord(new Vector2(initcoord.x, 1), true);
                            initcoord = pointer_unit_initCoord;
                            //大きいユニットのチェック用のフラグを立てる
                            isBig = true;
                            //ユニットがあれば、認識する
                            unitIsGrabbed = true;
                        }

                        //D[X,1]にTypeDのユニットがない場合は、D[X,2]にユニットがあるかも。であれば、取得する。
                        else if (GameObject.Find("gt_" + int.Parse(initcoord.x.ToString()) + "d2").transform.childCount > 0)
                        {
                            //ユニットのもとの座標をオーバーライドする
                            SetPointerUnitOriginalCoord(new Vector2(initcoord.x, 2), true);
                            initcoord = pointer_unit_initCoord;
                            //大きいユニットのチェック用のフラグを立てる
                            isBig = true;
                            //ユニットがあれば、認識する
                            unitIsGrabbed = true;
                        }
                        break;
                    //真ん中のカラム3 　S[x,3]
                    case 3:

                        //D[X,2]にユニットがあれば、取得する
                        if (GameObject.Find("gt_" + int.Parse(initcoord.x.ToString()) + "d2").transform.childCount > 0)
                        {
                            //ユニットのもとの座標をオーバーライドする
                            SetPointerUnitOriginalCoord(new Vector2(initcoord.x, 2), true);
                            initcoord = pointer_unit_initCoord;
                            //大きいユニットのチェック用のフラグを立てる
                            isBig = true;
                            //ユニットがあれば、認識する
                            unitIsGrabbed = true;
                        }
                        //D[X,2]にTypeDのユニットがない場合は、D[X,3]にユニットがあるかも。であれば、取得する。
                        else if (GameObject.Find("gt_" + int.Parse(initcoord.x.ToString()) + "d3").transform.childCount > 0)
                        {
                            //ユニットのもとの座標をオーバーライドする
                            SetPointerUnitOriginalCoord(new Vector2(initcoord.x, 3), true);
                            initcoord = pointer_unit_initCoord;
                            //大きいユニットのチェック用のフラグを立てる
                            isBig = true;
                            //ユニットがあれば、認識する
                            unitIsGrabbed = true;
                        }
                        break;
                    //右のカラム(カラム4) 　S[x,4]
                    case 4:
                        //D[X,2]にユニットがあれば、取得する
                        if (GameObject.Find("gt_" + int.Parse(initcoord.x.ToString()) + "d3").transform.childCount > 0)
                        {
                            //ユニットのもとの座標をオーバーライドする
                            SetPointerUnitOriginalCoord(new Vector2(initcoord.x, 3), true);
                            initcoord = pointer_unit_initCoord;
                            //大きいユニットのチェック用のフラグを立てる
                            isBig = true;
                            //ユニットがあれば、認識する
                            unitIsGrabbed = true;
                        }
                        break;
                }
                //大きいユニットじゃない場合は、ユニットがないので、選択したユニットのtransformをリセットとリターン
                if (!isBig)
                {
                    changeUnitCoord_Process_UnitGrabbedTrans = null;    //グリッドでユニットを移動するときに、移動しようとしているユニットのトランスフォームをここに格納する
                    unitGrabbed_IsTypeD = false;                        //取得されるユニットのタイプSD値をリセットする
                    return;
                }
            }

            //ユニットがある場合、認識する
            else
                unitIsGrabbed = true;

            //ユニットがある場合、取得する
            if (unitIsGrabbed)
            {
                //ユニットタイプの認識
                string s_unitTypeSD = null;

                if (isBig)
                    s_unitTypeSD = unitTypeSD_IsD;

                else
                    s_unitTypeSD = unitTypeSD_IsS;


                //ユニットのtransformを取得して
                changeUnitCoord_Process_UnitGrabbedTrans = GameObject.Find("gt_" + int.Parse(initcoord.x.ToString()) + s_unitTypeSD + int.Parse(initcoord.y.ToString())).transform.GetChild(0);
                //ユニットの取得が出来た場合は、フラグを立てる
                if (changeUnitCoord_Process_UnitGrabbedTrans != null)
                {
                    go_selectedUnit = changeUnitCoord_Process_UnitGrabbedTrans.gameObject;
                    


                    //敵の選択のサイン
                    PlayerUnit pu_chara = GameObject.Find("pu_" + go_selectedUnit.name).GetComponent<PlayerUnit>();
                    EnemyUnit eu_enemy = GameObject.Find("pu_enemy0" + pu_chara.enemy.ToString()).GetComponent<EnemyUnit>();

                    if (eu_enemy.isAlive == false)
                    {
                        pu_chara.CheckandAutoSwitchEnemy();
                    }

                    //ボードの上にあるHUDの更新（ユニット）
                    UpdateTeamUnit2DHUD(go_selectedUnit);

                    //ユニット選択のサイン
                    charIndicator.transform.position = new Vector3(
    changeUnitCoord_Process_UnitGrabbedTrans.position.x, 4.5f, changeUnitCoord_Process_UnitGrabbedTrans.position.z);



                    SetChangeUnitCoord_Process_UnitGrabbed(true);
                    if (isBig)
                        unitGrabbed_IsTypeD = true;
                    else
                        unitGrabbed_IsTypeD = false;
                }
            }
        }

        Debug.Log("ユニットのoriginal座標(選択したユニットのもとの座標)。ユニットの認識後。" + initcoord + " 。　※注意：一回サイクルがkな量になったら、TypeDのオーバーライドがありかもしれない。");
        Debug.Log("ユニットのfinal座標（移動の対処の座標）。ユニットの認識後。" + finalcoord + " 。オーバーライドがないから、いつもポインターから取得したデータをここで確認するのが可能。");
        Debug.Log("ユニットの取得ができた？ " + changeUnitCoord_Process_UnitGrabbed + " : " + changeUnitCoord_Process_UnitGrabbedTrans.name);

        //エラーチェック（グリッド外の選択）
        if (finalcoord == Vector2.zero)
            return;

        if (performActionFlag)
            return;

        //斜めのケース
        bool waitfornaname = false;
        bool readytorender = true;

        //行とカラムの変更のチェック
        if (pointer_unit_initCoord_beforeOverride != finalcoord)
        {
            unit_naname_move_frame_buffer_count = 0;
            waitfornaname = true;
            readytorender = false;
            //行とカラムの変更（斜め）
            if (pointer_unit_initCoord_beforeOverride.x != finalcoord.x && pointer_unit_initCoord_beforeOverride.y != finalcoord.y)
            {
                unit_naname_move_frame_buffer_max = unit_naname_move_frame_buffer_max_2axis;
            }
            else
                unit_naname_move_frame_buffer_max = unit_naname_move_frame_buffer_max_1axis;
        }

        //斜めの処理
        if (waitfornaname)
        {
            //移動を行うためのフレームのバッファーの更新
            unit_naname_move_frame_buffer_count++;

            //移動が行える
            if (unit_naname_move_frame_buffer_count >= unit_naname_move_frame_buffer_max)
            {
                //TypeDのマスの中の斜めの処理の認識
                if (unit_naname_move_frame_buffer_count == unit_naname_move_frame_buffer_max)
                {
                    if (pointer_unit_initCoord_beforeOverride.x != finalcoord.x)
                    {
                        switch (int.Parse(finalcoord.y.ToString()))
                        {
                            case 1:
                                if (pointer_unit_initCoord_beforeOverride.y == 2)
                                    nanameInsideSameTypeDMass = true;
                                break;
                            case 2:
                                if (pointer_unit_initCoord_beforeOverride.y == 1 || pointer_unit_initCoord_beforeOverride.y == 3)
                                    nanameInsideSameTypeDMass = true;
                                break;
                            case 3:
                                if (pointer_unit_initCoord_beforeOverride.y == 2 || pointer_unit_initCoord_beforeOverride.y == 4)
                                    nanameInsideSameTypeDMass = true;
                                break;
                            case 4:
                                if (pointer_unit_initCoord_beforeOverride.y == 3)
                                    nanameInsideSameTypeDMass = true;
                                break;
                        }
                    }
                }
                pointer_unit_initCoord_beforeOverride = finalcoord;
                readytorender = true;
            }
        }

        //ユニットの取得は成功であれば、移動を行う
        if (changeUnitCoord_Process_UnitGrabbedTrans != null)
        {
            //*********************************************
            //ユニットの位置の変更だけ
            //*********************************************
            #region //ユニットの位置の変更だけ

            //ユニットタイプの認識
            string s_unitTypeSD = null;

            //ユニットタイプSDで処理の分裂を定義する
            switch (unitGrabbed_IsTypeD)
            {
                //普通
                case false:
                    s_unitTypeSD = unitTypeSD_IsS;

                    break;
                //大きい
                case true:
                    s_unitTypeSD = unitTypeSD_IsD;

                    //対象のマスを認識する：　finalcoordをTypeS⇒TypeD
                    //      ※注意：この時点で、initcoordsはType Dの空間に変換された
                    switch (int.Parse(finalcoord.y.ToString()))
                    {
                        //⇒S[x,1]
                        case 1:
                            #region //D[X,1]⇒S[x,1]
                            if (initcoord.y == 1)
                            {
                                //S[x,1]⇒S[x,1]
                                if (pointer_unit_initCoord_beforeOverride_TypeD2.y == 1)
                                    finalcoord = new Vector2(finalcoord.x, 1);
                                //S[x,2]⇒S[x,1]                       
                                if (pointer_unit_initCoord_beforeOverride_TypeD2.y == 2)
                                {
                                    finalcoord = new Vector2(finalcoord.x, 1);
                                }
                            }
                            #endregion

                            #region //D[X,2]⇒S[x,1]
                            if (initcoord.y == 2)
                            {
                                //S[x,2]⇒S[x,1]
                                if (pointer_unit_initCoord_beforeOverride_TypeD2.y == 2)
                                    finalcoord = new Vector2(finalcoord.x, 1);
                                //S[x,3]⇒S[x,1]                       
                                if (pointer_unit_initCoord_beforeOverride_TypeD2.y == 3)
                                {
                                    finalcoord = new Vector2(finalcoord.x, 1);
                                }
                            }
                            #endregion

                            #region //D[X,3]⇒S[x,1]
                            if (initcoord.y == 3)
                            {
                                //S[x,3]⇒S[x,1]
                                if (pointer_unit_initCoord_beforeOverride_TypeD2.y == 3)
                                    finalcoord = new Vector2(finalcoord.x, 1);
                                //S[x,4]⇒S[x,1]                       
                                if (pointer_unit_initCoord_beforeOverride_TypeD2.y == 4)
                                {
                                    finalcoord = new Vector2(finalcoord.x, 1);
                                }
                            }
                            #endregion

                            break;

                        //⇒S[x,2]
                        case 2:
                            #region //D[X,1]⇒S[x,2]
                            if (initcoord.y == 1)
                            {
                                //S[x,1]⇒S[x,2]
                                if (pointer_unit_initCoord_beforeOverride_TypeD2.y == 1)
                                    finalcoord = new Vector2(finalcoord.x, 2);

                                //S[x,2]⇒S[x,2]
                                if (pointer_unit_initCoord_beforeOverride_TypeD2.y == 2)
                                {
                                    finalcoord = new Vector2(finalcoord.x, 1);
                                }
                            }
                            #endregion

                            #region //D[X,2]⇒S[x,2]
                            if (initcoord.y == 2)
                            {
                                //S[x,2]⇒S[x,2]
                                if (pointer_unit_initCoord_beforeOverride_TypeD2.y == 2)
                                    finalcoord = new Vector2(finalcoord.x, 2);
                                //S[x,3]⇒S[x,2]                       
                                if (pointer_unit_initCoord_beforeOverride_TypeD2.y == 3)
                                {
                                    finalcoord = new Vector2(finalcoord.x, 1);
                                }
                            }
                            #endregion

                            #region //D[X,3]⇒S[x,2]
                            if (initcoord.y == 3)
                            {
                                //S[x,3]⇒S[x,2]
                                if (pointer_unit_initCoord_beforeOverride_TypeD2.y == 3)
                                    finalcoord = new Vector2(finalcoord.x, 2);
                                //S[x,4]⇒S[x,2]                       
                                if (pointer_unit_initCoord_beforeOverride_TypeD2.y == 4)
                                {
                                    finalcoord = new Vector2(finalcoord.x, 1);
                                }
                            }
                            #endregion
                            break;
                        //⇒S[x,3]
                        case 3:
                            #region //D[X,1]⇒S[x,3]
                            if (initcoord.y == 1)
                            {
                                //S[x,1]⇒S[x,3]
                                if (pointer_unit_initCoord_beforeOverride_TypeD2.y == 1)
                                    finalcoord = new Vector2(finalcoord.x, 3);

                                //S[x,2]⇒S[x,3]
                                if (pointer_unit_initCoord_beforeOverride_TypeD2.y == 2)
                                {
                                    finalcoord = new Vector2(finalcoord.x, 2);
                                }
                            }
                            #endregion

                            #region //D[X,2]⇒S[x,3]
                            if (initcoord.y == 2)
                            {
                                //S[x,2]⇒S[x,3]
                                if (pointer_unit_initCoord_beforeOverride_TypeD2.y == 2)
                                    finalcoord = new Vector2(finalcoord.x, 3);
                                //S[x,3]⇒S[x,3]                       
                                if (pointer_unit_initCoord_beforeOverride_TypeD2.y == 3)
                                {
                                    finalcoord = new Vector2(finalcoord.x, 2);
                                }
                            }
                            #endregion

                            #region //D[X,3]⇒S[x,3]
                            if (initcoord.y == 3)
                            {
                                //S[x,3]⇒S[x,3]
                                if (pointer_unit_initCoord_beforeOverride_TypeD2.y == 3)
                                    finalcoord = new Vector2(finalcoord.x, 3);
                                //S[x,4]⇒S[x,3]                       
                                if (pointer_unit_initCoord_beforeOverride_TypeD2.y == 4)
                                {
                                    finalcoord = new Vector2(finalcoord.x, 2);
                                }
                            }
                            #endregion
                            break;
                        //⇒S[x,4]
                        case 4:
                            #region //D[X,1]⇒S[x,4]
                            if (initcoord.y == 1)
                            {
                                //S[x,1]⇒S[x,4]
                                if (pointer_unit_initCoord_beforeOverride_TypeD2.y == 1)
                                    finalcoord = new Vector2(finalcoord.x, 3);

                                //S[x,2]⇒S[x,4]
                                if (pointer_unit_initCoord_beforeOverride_TypeD2.y == 2)
                                {
                                    finalcoord = new Vector2(finalcoord.x, 3);
                                }
                            }
                            #endregion

                            #region //D[X,2]⇒S[x,4]
                            if (initcoord.y == 2)
                            {
                                //S[x,2]⇒S[x,4]
                                if (pointer_unit_initCoord_beforeOverride_TypeD2.y == 2)
                                    finalcoord = new Vector2(finalcoord.x, 3);
                                //S[x,3]⇒S[x,4]                       
                                if (pointer_unit_initCoord_beforeOverride_TypeD2.y == 3)
                                {
                                    finalcoord = new Vector2(finalcoord.x, 3);
                                }
                            }
                            #endregion

                            #region //D[X,3]⇒S[x,4]
                            if (initcoord.y == 3)
                            {
                                //S[x,3]⇒S[x,4]
                                if (pointer_unit_initCoord_beforeOverride_TypeD2.y == 3)
                                    finalcoord = new Vector2(finalcoord.x, 3);
                                //S[x,4]⇒S[x,4]                       
                                if (pointer_unit_initCoord_beforeOverride_TypeD2.y == 4)
                                {
                                    finalcoord = new Vector2(finalcoord.x, 3);
                                }
                            }
                            #endregion
                            break;
                    }
                    break;





            }

            Debug.Log("ユニットの移動の後のoriginal座標(選択したユニットのもとの座標)。ユニットの認識の前。" + initcoord + " 。　※注意：一回サイクルがkな量になったら、TypeDのオーバーライドがありかもしれない。");
            Debug.Log("ユニットの移動の後のfinal座標（移動の対処の座標）。ユニットの認識の前。" + finalcoord + " 。※注意：一回サイクルがkな量になったら、TypeDのオーバーライドがありかもしれない。");

            #endregion




            //*********************************************
            //ユニットの位置の交換の場合
            //*********************************************

            #region　//ユニットの位置の交換の場合

            //対象の座標は全フレームと一緒なら、何も変わってないので、ユニットの位置の交換をしなくて良い
            if (finalcoord != unit_Move_LastPosition)
            {

                //斜めの処理を終わらせてから、処理させる
                if (unit_naname_move_frame_buffer_count >= unit_naname_move_frame_buffer_max)
                {

                    //対象のマスにはユニットの存在を確認する

                    //存在のチェックが出来たら、unitInDestinyMassCheck_Coordに対象の座標をコピーする（これで、対象の座標の変更の時だけに、ユニットの存在を確認する）
                    if (unitInDestinyMassCheck_Coord != finalcoord)
                    {
                        GameObject massNode = null; //分析するノード
                        GameObject unitNode = null; //分析で見つけたユニット

                        #region //TypeDのユニットを移動している場合
                        if (unitGrabbed_IsTypeD)
                        {
                            //TypeD[X,Y]のユニットの存在のチェック

                            #region I. 対象の座標にユニットがある
                            massNode = GameObject.Find("gt_" + int.Parse(finalcoord.x.ToString()) + unitTypeSD_IsD + int.Parse(finalcoord.y.ToString()));

                            //ユニットが見つかった場合
                            if (massNode.transform.childCount > 0 && massNode.transform.GetChild(0).gameObject.name != changeUnitCoord_Process_UnitGrabbedTrans.gameObject.name)
                            {
                                //ユニットを取得
                                unitNode = massNode.transform.GetChild(0).transform.gameObject;

                                //移動しているユニットの情報を取得する
                                string movingUnitCoordName = changeUnitCoord_Process_UnitGrabbedTrans.parent.gameObject.name;
                                string movingUnitCoord_X = movingUnitCoordName.Substring(movingUnitCoordName.Length - 3)[0].ToString();
                                string movingUnitCoord_Y = movingUnitCoordName.Substring(movingUnitCoordName.Length - 1).ToString();
                                string movingUnitName = changeUnitCoord_Process_UnitGrabbedTrans.gameObject.name;

                                //ユニットの移動
                                unitNode.transform.parent = changeUnitCoord_Process_UnitGrabbedTrans.parent;
                                unitNode.transform.localPosition = new Vector3(0, 0, 0);
                                unitNode.transform.rotation = Quaternion.Euler(23, 0, 0);
                            }
                            #endregion

                            #region II. 対象の座標にユニットがないけど、隣にユニットがある
                            else
                            {
                                //D[X,1]
                                if (finalcoord.y == 1)
                                {
                                    massNode = GameObject.Find("gt_" + int.Parse(finalcoord.x.ToString()) + unitTypeSD_IsD + "2");
                                    //ユニットが見つかった場合
                                    if (massNode.transform.childCount > 0)
                                    {
                                        //ユニットを取得
                                        unitNode = massNode.transform.GetChild(0).transform.gameObject;


                                        //移動しているユニットの情報を取得する
                                        string movingUnitCoordName = changeUnitCoord_Process_UnitGrabbedTrans.parent.gameObject.name;
                                        string movingUnitCoord_X = movingUnitCoordName.Substring(movingUnitCoordName.Length - 3)[0].ToString();
                                        string movingUnitCoord_Y = movingUnitCoordName.Substring(movingUnitCoordName.Length - 1).ToString();
                                        string movingUnitName = changeUnitCoord_Process_UnitGrabbedTrans.gameObject.name;

                                        //ユニットの移動
                                        unitNode.transform.parent = changeUnitCoord_Process_UnitGrabbedTrans.parent;
                                        unitNode.transform.localPosition = new Vector3(0, 0, 0);
                                        unitNode.transform.rotation = Quaternion.Euler(23, 0, 0);

                                        //TypeSのユニットが回りにあるかどうか確認する
                                        //上への動き　or //下への動き
                                        //D[X,1]
                                        massNode = GameObject.Find("gt_" + int.Parse(finalcoord.x.ToString()) + unitTypeSD_IsS + "1");

                                        //ユニットが見つかった場合
                                        if (massNode.transform.childCount > 0)
                                        {
                                            //ユニットを取得
                                            unitNode = massNode.transform.GetChild(0).transform.gameObject;
                                            unitNode.transform.parent = GameObject.Find("gt_" + finalcoord.x + unitTypeSD_IsS + "3").transform;
                                            unitNode.transform.localPosition = new Vector3(0, 0, 0);
                                            unitNode.transform.rotation = Quaternion.Euler(23, 0, 0);
                                        }
                                    }
                                }

                                //D[X,2]
                                else if (finalcoord.y == 2)
                                {
                                    #region A. 横の移動以外
                                    if (initcoord.x != finalcoord.x)
                                    {
                                        bool massNodeLeft = false;
                                        bool massNodeRight = false;

                                        //移動しているユニットの情報を取得する
                                        string movingUnitCoordName = changeUnitCoord_Process_UnitGrabbedTrans.parent.gameObject.name;
                                        string movingUnitCoord_X = movingUnitCoordName.Substring(movingUnitCoordName.Length - 3)[0].ToString();
                                        string movingUnitCoord_Y = movingUnitCoordName.Substring(movingUnitCoordName.Length - 1).ToString();
                                        string movingUnitName = changeUnitCoord_Process_UnitGrabbedTrans.gameObject.name;

                                        //左の座標
                                        massNode = GameObject.Find("gt_" + int.Parse(finalcoord.x.ToString()) + unitTypeSD_IsD + "1");
                                        //ユニットが見つかった場合
                                        if (massNode.transform.childCount > 0)
                                            massNodeLeft = true;

                                        //右の座標
                                        massNode = GameObject.Find("gt_" + int.Parse(finalcoord.x.ToString()) + unitTypeSD_IsD + "3");
                                        //ユニットが見つかった場合
                                        if (massNode.transform.childCount > 0)
                                            massNodeRight = true;

                                        if (massNodeLeft || massNodeRight)
                                        {
                                            if (massNodeLeft)
                                            {
                                                massNode = GameObject.Find("gt_" + int.Parse(finalcoord.x.ToString()) + unitTypeSD_IsD + "1");
                                                //ユニットを取得
                                                unitNode = massNode.transform.GetChild(0).transform.gameObject;

                                                //同じユニットで空のマスに横の動きのブロックが内容に
                                                if (changeUnitCoord_Process_UnitGrabbedTrans.gameObject.name != unitNode.name)
                                                {
                                                    //ユニットの移動
                                                    unitNode.transform.parent = changeUnitCoord_Process_UnitGrabbedTrans.parent;
                                                    unitNode.transform.localPosition = new Vector3(0, 0, 0);
                                                    unitNode.transform.rotation = Quaternion.Euler(23, 0, 0);
                                                }
                                                else
                                                    massNodeLeft = false;

                                            }

                                            else if (massNodeRight)
                                            {
                                                massNode = GameObject.Find("gt_" + int.Parse(finalcoord.x.ToString()) + unitTypeSD_IsD + "3");
                                                //ユニットを取得
                                                unitNode = massNode.transform.GetChild(0).transform.gameObject;
                                                //同じユニットで空のマスに横の動きのブロックが内容に
                                                if (changeUnitCoord_Process_UnitGrabbedTrans.gameObject.name != unitNode.name)
                                                {
                                                    //ユニットの移動
                                                    unitNode.transform.parent = changeUnitCoord_Process_UnitGrabbedTrans.parent;
                                                    unitNode.transform.localPosition = new Vector3(0, 0, 0);
                                                    unitNode.transform.rotation = Quaternion.Euler(23, 0, 0);
                                                }
                                                else
                                                    massNodeRight = false;
                                            }
                                        }

                                        //行はTypeDで満員なら、動かさない
                                        if (massNodeLeft && massNodeRight)
                                            finalcoord = new Vector2(int.Parse(movingUnitCoord_X), int.Parse(movingUnitCoord_Y));
                                        //TypeSのユニットが回りにあるかどうか確認する
                                        //上への動き　or //下への動き
                                        //D[X,2]                                  
                                        if (finalcoord.x > int.Parse(movingUnitCoord_X))
                                        {
                                            massNode = GameObject.Find("gt_" + int.Parse(finalcoord.x.ToString()) + unitTypeSD_IsS + "3");

                                            //ユニットが見つかった場合
                                            if (massNode.transform.childCount > 0)
                                            {
                                                //TypeDのユニットを動かしていないと言う確認
                                                if (changeUnitCoord_Process_UnitGrabbedTrans.parent.gameObject.name.Contains("s"))
                                                {
                                                    //ユニットを取得
                                                    unitNode = massNode.transform.GetChild(0).transform.gameObject;
                                                    unitNode.transform.parent = GameObject.Find("gt_" + finalcoord.x + unitTypeSD_IsS + "1").transform;
                                                    unitNode.transform.localPosition = new Vector3(0, 0, 0);
                                                    unitNode.transform.rotation = Quaternion.Euler(23, 0, 0);
                                                }
                                            }

                                            massNode = GameObject.Find("gt_" + int.Parse(finalcoord.x.ToString()) + unitTypeSD_IsS + "2");

                                            //ユニットが見つかった場合
                                            if (massNode.transform.childCount > 0)
                                            {
                                                //Typeのユニットを動かしていないと言う確認
                                                if (changeUnitCoord_Process_UnitGrabbedTrans.parent.gameObject.name.Contains("s"))
                                                {
                                                    //ユニットを取得
                                                    unitNode = massNode.transform.GetChild(0).transform.gameObject;
                                                    unitNode.transform.parent = GameObject.Find("gt_" + finalcoord.x + unitTypeSD_IsS + "4").transform;
                                                    unitNode.transform.localPosition = new Vector3(0, 0, 0);
                                                    unitNode.transform.rotation = Quaternion.Euler(23, 0, 0);
                                                }
                                            }
                                        }
                                    }
                                    #endregion

                                    #region B. 横の移動
                                    if (initcoord.x == finalcoord.x)
                                    {
                                        // D[X,1]
                                        if (initcoord.y == 1)
                                        {
                                            massNode = GameObject.Find("gt_" + int.Parse(finalcoord.x.ToString()) + unitTypeSD_IsD + "3");
                                            //ユニットが見つかった場合
                                            if (massNode.transform.childCount > 0)
                                            {
                                                //ユニットを取得
                                                unitNode = massNode.transform.GetChild(0).transform.gameObject;

                                                //移動しているユニットの情報を取得する
                                                string movingUnitCoordName = changeUnitCoord_Process_UnitGrabbedTrans.parent.gameObject.name;
                                                string movingUnitCoord_X = movingUnitCoordName.Substring(movingUnitCoordName.Length - 3)[0].ToString();
                                                string movingUnitCoord_Y = movingUnitCoordName.Substring(movingUnitCoordName.Length - 1).ToString();
                                                string movingUnitName = changeUnitCoord_Process_UnitGrabbedTrans.gameObject.name;

                                                //ユニットの移動
                                                unitNode.transform.parent = changeUnitCoord_Process_UnitGrabbedTrans.parent;
                                                unitNode.transform.localPosition = new Vector3(0, 0, 0);
                                                unitNode.transform.rotation = Quaternion.Euler(23, 0, 0);

                                                finalcoord = new Vector2(finalcoord.x, 3);
                                            }
                                        }
                                        // D[X,3]
                                        if (initcoord.y == 3)
                                        {
                                            massNode = GameObject.Find("gt_" + int.Parse(finalcoord.x.ToString()) + unitTypeSD_IsD + "1");
                                            //ユニットが見つかった場合
                                            if (massNode.transform.childCount > 0)
                                            {
                                                //ユニットを取得
                                                unitNode = massNode.transform.GetChild(0).transform.gameObject;


                                                //移動しているユニットの情報を取得する
                                                string movingUnitCoordName = changeUnitCoord_Process_UnitGrabbedTrans.parent.gameObject.name;
                                                string movingUnitCoord_X = movingUnitCoordName.Substring(movingUnitCoordName.Length - 3)[0].ToString();
                                                string movingUnitCoord_Y = movingUnitCoordName.Substring(movingUnitCoordName.Length - 1).ToString();
                                                string movingUnitName = changeUnitCoord_Process_UnitGrabbedTrans.gameObject.name;
                                                //ユニットの移動
                                                unitNode.transform.parent = changeUnitCoord_Process_UnitGrabbedTrans.parent;
                                                unitNode.transform.localPosition = new Vector3(0, 0, 0);
                                                unitNode.transform.rotation = Quaternion.Euler(23, 0, 0);

                                                finalcoord = new Vector2(finalcoord.x, 1);
                                            }
                                        }
                                    }
                                    #endregion
                                }
                                //D[X,3]
                                else if (finalcoord.y == 3)
                                {
                                    massNode = GameObject.Find("gt_" + int.Parse(finalcoord.x.ToString()) + unitTypeSD_IsD + "2");
                                    //ユニットが見つかった場合
                                    if (massNode.transform.childCount > 0)
                                    {
                                        //ユニットを取得
                                        unitNode = massNode.transform.GetChild(0).transform.gameObject;


                                        //移動しているユニットの情報を取得する
                                        string movingUnitCoordName = changeUnitCoord_Process_UnitGrabbedTrans.parent.gameObject.name;
                                        string movingUnitCoord_X = movingUnitCoordName.Substring(movingUnitCoordName.Length - 3)[0].ToString();
                                        string movingUnitCoord_Y = movingUnitCoordName.Substring(movingUnitCoordName.Length - 1).ToString();
                                        string movingUnitName = changeUnitCoord_Process_UnitGrabbedTrans.gameObject.name;

                                        //ユニットの移動
                                        unitNode.transform.parent = changeUnitCoord_Process_UnitGrabbedTrans.parent;
                                        unitNode.transform.localPosition = new Vector3(0, 0, 0);
                                        unitNode.transform.rotation = Quaternion.Euler(23, 0, 0);

                                        //TypeSのユニットが回りにあるかどうか確認する
                                        //上への動き　or //下への動き
                                        //D[X,3]
                                        massNode = GameObject.Find("gt_" + int.Parse(finalcoord.x.ToString()) + unitTypeSD_IsS + "4");

                                        //ユニットが見つかった場合
                                        if (massNode.transform.childCount > 0)
                                        {
                                            //ユニットを取得
                                            unitNode = massNode.transform.GetChild(0).transform.gameObject;
                                            unitNode.transform.parent = GameObject.Find("gt_" + finalcoord.x + unitTypeSD_IsS + "2").transform;
                                            unitNode.transform.localPosition = new Vector3(0, 0, 0);
                                            unitNode.transform.rotation = Quaternion.Euler(23, 0, 0);
                                        }
                                    }
                                }
                            }
                            #endregion

                            //TypeS[x,y]のユニットの存在のチェック

                            #region III. TypeS[x,y]


                            // パラメータ
                            bool unitInMassTypeS_A = false;
                            bool unitInMassTypeS_B = false;

                            string movingUnitCoordName_DCheck = null;
                            string movingUnitCoord_X_DCheck = null;

                            string movingUnitCoord_Y_DCheck = null;

                            // ⇒//D[X,Y]
                            switch (int.Parse(finalcoord.y.ToString()))
                            {
                                #region //D[X,1] = [x,1]と[x,2]
                                case 1:
                                    //処理するユニット数の認識
                                    unitInMassTypeS_A = false;
                                    unitInMassTypeS_B = false;

                                    massNode = GameObject.Find("gt_" + int.Parse(finalcoord.x.ToString()) + unitTypeSD_IsS + "1");
                                    if (massNode.transform.childCount > 0)
                                        unitInMassTypeS_A = true;

                                    massNode = GameObject.Find("gt_" + int.Parse(finalcoord.x.ToString()) + unitTypeSD_IsS + "2");
                                    if (massNode.transform.childCount > 0)
                                        unitInMassTypeS_B = true;



                                    movingUnitCoordName_DCheck = null;
                                    movingUnitCoord_X_DCheck = null;
                                    movingUnitCoord_Y_DCheck = null;

                                    //移動しているユニットの情報を取得する
                                    if (unitInMassTypeS_A == true || unitInMassTypeS_B == true)
                                    {
                                        //移動しているユニットの情報を取得する
                                        movingUnitCoordName_DCheck = changeUnitCoord_Process_UnitGrabbedTrans.parent.gameObject.name;
                                        movingUnitCoord_X_DCheck = movingUnitCoordName_DCheck.Substring(movingUnitCoordName_DCheck.Length - 3)[0].ToString();
                                        movingUnitCoord_Y_DCheck = movingUnitCoordName_DCheck.Substring(movingUnitCoordName_DCheck.Length - 1).ToString();
                                    }

                                    //S[x,1]のオブジェクトの処理
                                    if (unitInMassTypeS_A)
                                    {
                                        massNode = GameObject.Find("gt_" + int.Parse(finalcoord.x.ToString()) + unitTypeSD_IsS + "1");
                                        //対象の座標にあるユニットの移動(階層の変更)
                                        unitNode = massNode.transform.GetChild(0).gameObject;

                                        string UnitNodeNewCoord_Y = null;

                                        //移動しているユニットの位置を認識する
                                        switch (movingUnitCoord_Y_DCheck)
                                        {
                                            case "1":
                                                UnitNodeNewCoord_Y = "1";
                                                break;
                                            case "2":
                                                //横
                                                if (int.Parse(movingUnitCoord_X_DCheck) == finalcoord.x)
                                                    UnitNodeNewCoord_Y = "3";
                                                //縦
                                                else
                                                    UnitNodeNewCoord_Y = "2";
                                                break;
                                            case "3":
                                                UnitNodeNewCoord_Y = "3";
                                                break;
                                        }

                                        //対象の座標にユニットを移動しているユニットの階層へ
                                        unitNode.transform.parent = GameObject.Find("gt_" + movingUnitCoord_X_DCheck + unitTypeSD_IsS + UnitNodeNewCoord_Y).transform;
                                        unitNode.transform.localPosition = new Vector3(0, 0, 0);
                                        unitNode.transform.rotation = Quaternion.Euler(23, 0, 0);
                                    }

                                    //S[x,2]のオブジェクトの処理
                                    if (unitInMassTypeS_B)
                                    {
                                        massNode = GameObject.Find("gt_" + int.Parse(finalcoord.x.ToString()) + unitTypeSD_IsS + "2");
                                        //対象の座標にあるユニットの移動(階層の変更)
                                        unitNode = massNode.transform.GetChild(0).gameObject;

                                        string UnitNodeNewCoord_Y = null;

                                        //移動しているユニットの位置を認識する
                                        switch (movingUnitCoord_Y_DCheck)
                                        {
                                            case "1":
                                                UnitNodeNewCoord_Y = "2";
                                                break;
                                            case "2":
                                                //横
                                                if (int.Parse(movingUnitCoord_X_DCheck) == finalcoord.x)
                                                    UnitNodeNewCoord_Y = "4";
                                                //縦
                                                else
                                                    UnitNodeNewCoord_Y = "3";
                                                break;
                                            case "3":
                                                UnitNodeNewCoord_Y = "4";
                                                break;
                                        }

                                        //対象の座標にユニットを移動しているユニットの階層へ
                                        unitNode.transform.parent = GameObject.Find("gt_" + movingUnitCoord_X_DCheck + unitTypeSD_IsS + UnitNodeNewCoord_Y).transform;
                                        unitNode.transform.localPosition = new Vector3(0, 0, 0);
                                        unitNode.transform.rotation = Quaternion.Euler(23, 0, 0);
                                    }
                                    break;
                                #endregion

                                #region //D[X,2] = [x,2]と[x,3]
                                case 2:

                                    unitInMassTypeS_A = false;
                                    unitInMassTypeS_B = false;
                                    //処理するユニット数の認識
                                    massNode = GameObject.Find("gt_" + int.Parse(finalcoord.x.ToString()) + unitTypeSD_IsS + "2");
                                    if (massNode.transform.childCount > 0)
                                        unitInMassTypeS_A = true;
                                    massNode = GameObject.Find("gt_" + int.Parse(finalcoord.x.ToString()) + unitTypeSD_IsS + "3");
                                    if (massNode.transform.childCount > 0)
                                        unitInMassTypeS_B = true;

                                    movingUnitCoordName_DCheck = null;
                                    movingUnitCoord_X_DCheck = null;
                                    movingUnitCoord_Y_DCheck = null;

                                    //移動しているユニットの情報を取得する
                                    if (unitInMassTypeS_A == true || unitInMassTypeS_B == true)
                                    {
                                        //移動しているユニットの情報を取得する
                                        movingUnitCoordName_DCheck = changeUnitCoord_Process_UnitGrabbedTrans.parent.gameObject.name;
                                        movingUnitCoord_X_DCheck = movingUnitCoordName_DCheck.Substring(movingUnitCoordName_DCheck.Length - 3)[0].ToString();
                                        movingUnitCoord_Y_DCheck = movingUnitCoordName_DCheck.Substring(movingUnitCoordName_DCheck.Length - 1).ToString();
                                    }

                                    //S[x,2]のオブジェクトの処理
                                    if (unitInMassTypeS_A)
                                    {
                                        massNode = GameObject.Find("gt_" + int.Parse(finalcoord.x.ToString()) + unitTypeSD_IsS + "2");
                                        //対象の座標にあるユニットの移動(階層の変更)
                                        unitNode = massNode.transform.GetChild(0).gameObject;

                                        string UnitNodeNewCoord_Y = null;

                                        //移動しているユニットの位置を認識する
                                        switch (movingUnitCoord_Y_DCheck)
                                        {
                                            // D[X,1]
                                            case "1":
                                                UnitNodeNewCoord_Y = "1";
                                                break;
                                            // D[X,2]
                                            case "2":
                                                //横
                                                if (int.Parse(movingUnitCoord_X_DCheck) == finalcoord.x)
                                                    UnitNodeNewCoord_Y = "1";
                                                //縦
                                                else
                                                    UnitNodeNewCoord_Y = "2";
                                                break;
                                            // D[X,3]
                                            case "3":
                                                if (int.Parse(movingUnitCoord_X_DCheck) == finalcoord.x)
                                                    UnitNodeNewCoord_Y = "4";
                                                else
                                                    UnitNodeNewCoord_Y = "3";
                                                break;
                                        }

                                        //対象の座標にユニットを移動しているユニットの階層へ
                                        unitNode.transform.parent = GameObject.Find("gt_" + movingUnitCoord_X_DCheck + unitTypeSD_IsS + UnitNodeNewCoord_Y).transform;

                                        //変更を行う
                                        unitNode.transform.localPosition = new Vector3(0, 0, 0);
                                        unitNode.transform.rotation = Quaternion.Euler(23, 0, 0);
                                    }

                                    //S[x,3]のオブジェクトの処理
                                    if (unitInMassTypeS_B)
                                    {
                                        massNode = GameObject.Find("gt_" + int.Parse(finalcoord.x.ToString()) + unitTypeSD_IsS + "3");
                                        //対象の座標にあるユニットの移動(階層の変更)
                                        unitNode = massNode.transform.GetChild(0).gameObject;

                                        string UnitNodeNewCoord_Y = null;

                                        //移動しているユニットの位置を認識する
                                        switch (movingUnitCoord_Y_DCheck)
                                        {
                                            case "1":
                                                if (int.Parse(movingUnitCoord_X_DCheck) == finalcoord.x)
                                                    UnitNodeNewCoord_Y = "1";
                                                else
                                                    UnitNodeNewCoord_Y = "2";
                                                break;
                                            case "2":
                                                //横
                                                if (int.Parse(movingUnitCoord_X_DCheck) == finalcoord.x)
                                                    UnitNodeNewCoord_Y = "2";
                                                //縦
                                                else
                                                    UnitNodeNewCoord_Y = "3";
                                                break;
                                            case "3":
                                                UnitNodeNewCoord_Y = "4";
                                                break;
                                        }

                                        //対象の座標にユニットを移動しているユニットの階層へ
                                        unitNode.transform.parent = GameObject.Find("gt_" + movingUnitCoord_X_DCheck + unitTypeSD_IsS + UnitNodeNewCoord_Y).transform;

                                        //変更を行う
                                        unitNode.transform.localPosition = new Vector3(0, 0, 0);
                                        unitNode.transform.rotation = Quaternion.Euler(23, 0, 0);
                                    }
                                    break;
                                #endregion

                                #region //D[X,3] = [x,3]と[x,4]
                                case 3:
                                    //処理するユニット数の認識
                                    unitInMassTypeS_A = false;
                                    unitInMassTypeS_B = false;
                                    massNode = GameObject.Find("gt_" + int.Parse(finalcoord.x.ToString()) + unitTypeSD_IsS + "3");
                                    if (massNode.transform.childCount > 0)
                                        unitInMassTypeS_A = true;
                                    massNode = GameObject.Find("gt_" + int.Parse(finalcoord.x.ToString()) + unitTypeSD_IsS + "4");
                                    if (massNode.transform.childCount > 0)
                                        unitInMassTypeS_B = true;

                                    movingUnitCoordName_DCheck = null;
                                    movingUnitCoord_X_DCheck = null;
                                    movingUnitCoord_Y_DCheck = null;

                                    //移動しているユニットの情報を取得する
                                    if (unitInMassTypeS_A == true || unitInMassTypeS_B == true)
                                    {
                                        //移動しているユニットの情報を取得する
                                        movingUnitCoordName_DCheck = changeUnitCoord_Process_UnitGrabbedTrans.parent.gameObject.name;
                                        movingUnitCoord_X_DCheck = movingUnitCoordName_DCheck.Substring(movingUnitCoordName_DCheck.Length - 3)[0].ToString();
                                        movingUnitCoord_Y_DCheck = movingUnitCoordName_DCheck.Substring(movingUnitCoordName_DCheck.Length - 1).ToString();
                                    }

                                    //S[x,3]のオブジェクトの処理
                                    if (unitInMassTypeS_A)
                                    {
                                        massNode = GameObject.Find("gt_" + int.Parse(finalcoord.x.ToString()) + unitTypeSD_IsS + "3");
                                        //対象の座標にあるユニットの移動(階層の変更)
                                        unitNode = massNode.transform.GetChild(0).gameObject;

                                        string UnitNodeNewCoord_Y = null;

                                        //移動しているユニットの位置を認識する
                                        switch (movingUnitCoord_Y_DCheck)
                                        {
                                            case "1":
                                                UnitNodeNewCoord_Y = "1";
                                                break;
                                            case "2":
                                                //横
                                                if (int.Parse(movingUnitCoord_X_DCheck) == finalcoord.x)
                                                    UnitNodeNewCoord_Y = "1";
                                                //縦
                                                else
                                                    UnitNodeNewCoord_Y = "2";
                                                break;
                                            case "3":
                                                UnitNodeNewCoord_Y = "3";
                                                break;
                                        }

                                        //対象の座標にユニットを移動しているユニットの階層へ
                                        unitNode.transform.parent = GameObject.Find("gt_" + movingUnitCoord_X_DCheck + unitTypeSD_IsS + UnitNodeNewCoord_Y).transform;

                                        //変更を行う
                                        unitNode.transform.localPosition = new Vector3(0, 0, 0);
                                        unitNode.transform.rotation = Quaternion.Euler(23, 0, 0);
                                    }

                                    //S[x,4]のオブジェクトの処理
                                    if (unitInMassTypeS_B)
                                    {
                                        massNode = GameObject.Find("gt_" + int.Parse(finalcoord.x.ToString()) + unitTypeSD_IsS + "4");
                                        //対象の座標にあるユニットの移動(階層の変更)
                                        unitNode = massNode.transform.GetChild(0).gameObject;

                                        string UnitNodeNewCoord_Y = null;

                                        //移動しているユニットの位置を認識する
                                        switch (movingUnitCoord_Y_DCheck)
                                        {
                                            case "1":
                                                UnitNodeNewCoord_Y = "2";
                                                break;
                                            case "2":
                                                //横
                                                if (int.Parse(movingUnitCoord_X_DCheck) == finalcoord.x)
                                                    UnitNodeNewCoord_Y = "2";
                                                //縦
                                                else
                                                    UnitNodeNewCoord_Y = "3";
                                                break;
                                            case "3":
                                                UnitNodeNewCoord_Y = "4";
                                                break;
                                        }

                                        //対象の座標にユニットを移動しているユニットの階層へ
                                        unitNode.transform.parent = GameObject.Find("gt_" + movingUnitCoord_X_DCheck + unitTypeSD_IsS + UnitNodeNewCoord_Y).transform;

                                        //変更を行う
                                        unitNode.transform.localPosition = new Vector3(0, 0, 0);
                                        unitNode.transform.rotation = Quaternion.Euler(23, 0, 0);
                                    }
                                    break;
                                    #endregion
                            }
                            #endregion
                        }

                        #endregion



                        #region //TypeSユニットを移動している場合
                        else
                        {
                            //移動しているユニットのペアレントを取得
                            Transform unitGrabbedParentTransformParent = changeUnitCoord_Process_UnitGrabbedTrans.transform.parent;

                            //TypeSのユニットの存在のチェック
                            massNode = GameObject.Find("gt_" + int.Parse(finalcoord.x.ToString()) + unitTypeSD_IsS + int.Parse(finalcoord.y.ToString()));
                            if (massNode.transform.childCount > 0)
                            {
                                unitNode = massNode.transform.GetChild(0).gameObject;
                                unitNode.transform.parent = unitGrabbedParentTransformParent;
                            }


                            //TypeDのユニットの存在のチェック
                            if (unitNode == null)
                            {
                                //移動しているユニットの情報を取得する
                                string movingUnitCoordName_01 = changeUnitCoord_Process_UnitGrabbedTrans.parent.gameObject.name;
                                string movingUnitCoord_X_01 = movingUnitCoordName_01.Substring(movingUnitCoordName_01.Length - 3)[0].ToString();
                                string movingUnitCoord_Y_01 = movingUnitCoordName_01.Substring(movingUnitCoordName_01.Length - 1).ToString();
                                int movingUnitCoord_X_num = int.Parse(movingUnitCoord_X_01);
                                int movingUnitCoord_Y_num = int.Parse(movingUnitCoord_Y_01);

                                if (movingUnitCoord_X_num == finalcoord.x)
                                {
                                    //動かせるニットの情報を取得
                                    if (finalcoord.y == 2)
                                    {
                                        massNode = GameObject.Find("gt_" + finalcoord.x + unitTypeSD_IsD + "2");
                                        if (massNode.transform.childCount > 0)
                                        {   
                                            unitNode = massNode.transform.GetChild(0).gameObject;
                                            unitNode.transform.parent = GameObject.Find("gt_" + finalcoord.x + unitTypeSD_IsD + "2").transform;
                                            finalcoord.y = 1;
                                            massNode = GameObject.Find("gt_" + finalcoord.x + unitTypeSD_IsS + "1");
                                            if (massNode.transform.childCount > 0)
                                            {
                                                unitNode = massNode.transform.GetChild(0).gameObject;
                                                unitNode.transform.parent = GameObject.Find("gt_" + finalcoord.x + unitTypeSD_IsS + "4").transform;
                                                unitNode.transform.localPosition = new Vector3(0, 0, 0);
                                                unitNode.transform.rotation = Quaternion.Euler(23, 0, 0);
                                            }
                                        }
                                    }
                                    else if (finalcoord.y == 3)
                                    {
                                        massNode = GameObject.Find("gt_" + finalcoord.x + unitTypeSD_IsD + "2");
                                        if (massNode.transform.childCount > 0)
                                        {
                                            unitNode = massNode.transform.GetChild(0).gameObject;
                                            unitNode.transform.parent = GameObject.Find("gt_" + finalcoord.x + unitTypeSD_IsD + "2").transform;
                                            finalcoord.y = 4;
                                            massNode = GameObject.Find("gt_" + finalcoord.x + unitTypeSD_IsS + "1");
                                            if (massNode.transform.childCount > 0)
                                            {
                                                unitNode = massNode.transform.GetChild(0).gameObject;
                                                unitNode.transform.parent = GameObject.Find("gt_" + finalcoord.x + unitTypeSD_IsS + "1").transform;
                                                unitNode.transform.localPosition = new Vector3(0, 0, 0);
                                                unitNode.transform.rotation = Quaternion.Euler(23, 0, 0);
                                            }
                                        }
                                    }
                                    if (finalcoord.y == 2)
                                    {
                                        massNode = GameObject.Find("gt_" + finalcoord.x + unitTypeSD_IsD + "1");
                                        if (massNode.transform.childCount > 0)
                                        {
                                            unitNode = massNode.transform.GetChild(0).gameObject;
                                            unitNode.transform.parent = GameObject.Find("gt_" + finalcoord.x + unitTypeSD_IsD + "1").transform;
                                            finalcoord.y = 3;
                                        }
                                    }
                                    else if (finalcoord.y == 3)
                                    {
                                        massNode = GameObject.Find("gt_" + finalcoord.x + unitTypeSD_IsD + "3");
                                        if (massNode.transform.childCount > 0)
                                        {
                                            unitNode = massNode.transform.GetChild(0).gameObject;
                                            unitNode.transform.parent = GameObject.Find("gt_" + finalcoord.x + unitTypeSD_IsD + "3").transform;
                                            finalcoord.y = 2;
                                        }
                                    }
                                    if(finalcoord.y == 4)
                                    {
                                        massNode = GameObject.Find("gt_" + finalcoord.x + unitTypeSD_IsD + "3");
                                        if (massNode.transform.childCount > 0)
                                        {
                                            
                                            finalcoord.y = movingUnitCoord_Y_num;
                                        }
                                    }
                                    else if (finalcoord.y == 1)
                                    {
                                        massNode = GameObject.Find("gt_" + finalcoord.x + unitTypeSD_IsD + "1");
                                        if (massNode.transform.childCount > 0)
                                        {
                                            
                                            finalcoord.y = movingUnitCoord_Y_num;
                                        }
                                    }
                                }
  


                                switch (int.Parse(finalcoord.y.ToString()))
                                {
                                    //[x,1とx,2] = Dの[x,1]
                                    case 1:
                                        //動かせるニットの情報を取得
                                        massNode = GameObject.Find("gt_" + int.Parse(finalcoord.x.ToString()) + unitTypeSD_IsD + "1");
                                        if (massNode.transform.childCount > 0)
                                        {
                                            //対象の座標にあるユニットの移動(階層の変更)
                                            unitNode = massNode.transform.GetChild(0).gameObject;

                                            //移動しているユニットの情報を取得する
                                            string movingUnitCoordName = changeUnitCoord_Process_UnitGrabbedTrans.parent.gameObject.name;
                                            string movingUnitCoord_X = movingUnitCoordName.Substring(movingUnitCoordName.Length - 3)[0].ToString();
                                            string movingUnitCoord_Y = movingUnitCoordName.Substring(movingUnitCoordName.Length - 1).ToString();


                                            //移動させる前に、隣のマスを確認する
                                            int limit_Move = 4;

                                            //TYPE D 移動しているユニットのとなりのyのステータスを確認（空 or ユニットの存在）
                                            massNode = GameObject.Find("gt_" + movingUnitCoord_X + unitTypeSD_IsD + "2");
                                            //空じゃない場合は、移動を中止
                                            if (massNode.transform.childCount > 0)
                                            {
                                                limit_Move = int.Parse(movingUnitCoord_X);
                                                finalcoord.x = limit_Move; //動いていたユニットの処理をリバースする
                                            }
                                            else
                                            {
                                               unitNode.transform.parent = GameObject.Find("gt_" + movingUnitCoord_X + unitTypeSD_IsD + "1").transform;

                                                //if (int.Parse(movingUnitCoord_X) != limit_Move)
                                                //{
                                                    //移動しているユニットのとなりのyのステータスを確認（空 or ユニットの存在）
                                                    if (int.Parse(movingUnitCoordName.Substring(movingUnitCoordName.Length - 1).ToString()) == 1)
                                                        massNode = GameObject.Find("gt_" + movingUnitCoord_X + unitTypeSD_IsS + "2");
                                                    else
                                                        massNode = GameObject.Find("gt_" + movingUnitCoord_X + unitTypeSD_IsS + "1");

                                                    //からじゃない場合は、ユニットの移動を行う
                                                    if (massNode.transform.childCount > 0)
                                                    {
                                                        Transform unitNode02_transform = massNode.transform.GetChild(0);
                                                        unitNode02_transform.parent = GameObject.Find("gt_" + int.Parse(finalcoord.x.ToString()) + unitTypeSD_IsS + "2").transform;
                                                        unitNode02_transform.localPosition = new Vector3(0, 0, 0);
                                                        unitNode02_transform.rotation = Quaternion.Euler(23, 0, 0);
                                                    }
                                                //}
                                            }
                                        }
                                        break;

                                    //[x,1とx,2] = D[x,1] と　[x,2とx,3] = D[x,2]
                                    case 2:
                                        massNode = GameObject.Find("gt_" + int.Parse(finalcoord.x.ToString()) + unitTypeSD_IsD + "1");

                                        //[x,1とx,2]
                                        if (massNode.transform.childCount > 0)
                                        {
                                            //対象の座標にあるユニットの移動(階層の変更)
                                            unitNode = massNode.transform.GetChild(0).gameObject;

                                            //移動しているユニットの情報を取得する
                                            string movingUnitCoordName = changeUnitCoord_Process_UnitGrabbedTrans.parent.gameObject.name;
                                            string movingUnitCoord_X = movingUnitCoordName.Substring(movingUnitCoordName.Length - 3)[0].ToString();

                                            unitNode.transform.parent = GameObject.Find("gt_" + movingUnitCoord_X + unitTypeSD_IsD + "1").transform;

                                            //移動しているユニットのとなりのyのステータスを確認（空 or ユニットの存在）
                                            if (int.Parse(movingUnitCoordName.Substring(movingUnitCoordName.Length - 1).ToString()) == 2)
                                                massNode = GameObject.Find("gt_" + movingUnitCoord_X + unitTypeSD_IsS + "1");
                                            else
                                                massNode = GameObject.Find("gt_" + movingUnitCoord_X + unitTypeSD_IsS + "2");

                                            //からじゃない場合は、ユニットの移動を行う
                                            if (massNode.transform.childCount > 0)
                                            {
                                                Transform unitNode02_transform = massNode.transform.GetChild(0);
                                                unitNode02_transform.parent = GameObject.Find("gt_" + int.Parse(finalcoord.x.ToString()) + unitTypeSD_IsS + "1").transform;
                                                unitNode02_transform.localPosition = new Vector3(0, 0, 0);
                                                unitNode02_transform.rotation = Quaternion.Euler(23, 0, 0);
                                            }

                                            //TypeSのユニットはカバーされるかもしれないので、動かす
                                            massNode = GameObject.Find("gt_" + movingUnitCoord_X + unitTypeSD_IsS + "1");

                                            if (massNode.transform.childCount > 0)
                                            {
                                                Transform unitNode02_transform = massNode.transform.GetChild(0).transform;
                                                unitNode02_transform.parent = GameObject.Find("gt_" + movingUnitCoord_X + unitTypeSD_IsS + "3").transform;
                                                unitNode02_transform.localPosition = new Vector3(0, 0, 0);
                                                unitNode02_transform.rotation = Quaternion.Euler(23, 0, 0);
                                            }
                                        }

                                        //[x,2]と[x,3]
                                        else if (GameObject.Find("gt_" + int.Parse(finalcoord.x.ToString()) + unitTypeSD_IsD + "2").transform.childCount > 0)
                                        {
                                            //対象の座標にあるユニットの移動(階層の変更)
                                            massNode = GameObject.Find("gt_" + int.Parse(finalcoord.x.ToString()) + unitTypeSD_IsD + "2");
                                            unitNode = massNode.transform.GetChild(0).gameObject;

                                            //移動しているユニットの情報を取得する
                                            string movingUnitCoordName = changeUnitCoord_Process_UnitGrabbedTrans.parent.gameObject.name;
                                            string movingUnitCoord_X = movingUnitCoordName.Substring(movingUnitCoordName.Length - 3)[0].ToString();

                                            unitNode.transform.parent = GameObject.Find("gt_" + movingUnitCoord_X + unitTypeSD_IsD + "2").transform;



                                            //移動しているユニットのとなりのyのステータスを確認（空 or ユニットの存在）
                                            if (int.Parse(movingUnitCoordName.Substring(movingUnitCoordName.Length - 1).ToString()) == 3)
                                                massNode = GameObject.Find("gt_" + movingUnitCoord_X + unitTypeSD_IsS + "2");
                                            else
                                                massNode = GameObject.Find("gt_" + movingUnitCoord_X + unitTypeSD_IsS + "3");

                                            //からじゃない場合は、ユニットの移動を行う
                                            if (massNode.transform.childCount > 0)
                                            {
                                                Transform unitNode02_transform = massNode.transform.GetChild(0);
                                                unitNode02_transform.parent = GameObject.Find("gt_" + int.Parse(finalcoord.x.ToString()) + unitTypeSD_IsS + "3").transform;
                                                unitNode02_transform.localPosition = new Vector3(0, 0, 0);
                                                unitNode02_transform.rotation = Quaternion.Euler(23, 0, 0);
                                            }
                                            //D[X,2]                                  
                                            //TypeSのユニットはカバーされるかもしれないので、動かす
                                            massNode = GameObject.Find("gt_" + movingUnitCoord_X + unitTypeSD_IsS + "2");

                                            if (massNode.transform.childCount > 0)
                                            {
                                                Transform unitNode02_transform = massNode.transform.GetChild(0).transform;
                                                unitNode02_transform.parent = GameObject.Find("gt_" + movingUnitCoord_X + unitTypeSD_IsS + "1").transform;
                                                unitNode02_transform.localPosition = new Vector3(0, 0, 0);
                                                unitNode02_transform.rotation = Quaternion.Euler(23, 0, 0);
                                            }
                                        }
                                        break;

                                    //[x,2とx,3] = D[x,2]　と　[x,3とx,4] = D[x,3]
                                    case 3:

                                        massNode = GameObject.Find("gt_" + int.Parse(finalcoord.x.ToString()) + unitTypeSD_IsD + "2");

                                        //[x,2とx,3]
                                        if (massNode.transform.childCount > 0)
                                        {
                                            //対象の座標にあるユニットの移動(階層の変更)
                                            unitNode = massNode.transform.GetChild(0).gameObject;

                                            //移動しているユニットの情報を取得する
                                            string movingUnitCoordName = changeUnitCoord_Process_UnitGrabbedTrans.parent.gameObject.name;
                                            string movingUnitCoord_X = movingUnitCoordName.Substring(movingUnitCoordName.Length - 3)[0].ToString();

                                            unitNode.transform.parent = GameObject.Find("gt_" + movingUnitCoord_X + unitTypeSD_IsD + "2").transform;



                                            //移動しているユニットのとなりのyのステータスを確認（空 or ユニットの存在）
                                            if (int.Parse(movingUnitCoordName.Substring(movingUnitCoordName.Length - 1).ToString()) == 3)
                                                massNode = GameObject.Find("gt_" + movingUnitCoord_X + unitTypeSD_IsS + "2");
                                            else
                                                massNode = GameObject.Find("gt_" + movingUnitCoord_X + unitTypeSD_IsS + "3");

                                            //からじゃない場合は、ユニットの移動を行う
                                            if (massNode.transform.childCount > 0)
                                            {
                                                Transform unitNode02_transform = massNode.transform.GetChild(0);
                                                unitNode02_transform.parent = GameObject.Find("gt_" + int.Parse(finalcoord.x.ToString()) + unitTypeSD_IsS + "2").transform;
                                                unitNode02_transform.localPosition = new Vector3(0, 0, 0);
                                                unitNode02_transform.rotation = Quaternion.Euler(23, 0, 0);
                                            }

                                            //TypeSのユニットはカバーされるかもしれないので、動かす
                                            massNode = GameObject.Find("gt_" + movingUnitCoord_X + unitTypeSD_IsS + "2");

                                            if (massNode.transform.childCount > 0)
                                            {
                                                Transform unitNode02_transform = massNode.transform.GetChild(0).transform;
                                                unitNode02_transform.parent = GameObject.Find("gt_" + movingUnitCoord_X + unitTypeSD_IsS + "4").transform;
                                                unitNode02_transform.localPosition = new Vector3(0, 0, 0);
                                                unitNode02_transform.rotation = Quaternion.Euler(23, 0, 0);
                                            }
                                        }

                                        //[x,3とx,4]
                                        else if (GameObject.Find("gt_" + int.Parse(finalcoord.x.ToString()) + unitTypeSD_IsD + "3").transform.childCount > 0)
                                        {
                                            //対象の座標にあるユニットの移動(階層の変更)
                                            massNode = GameObject.Find("gt_" + int.Parse(finalcoord.x.ToString()) + unitTypeSD_IsD + "3");
                                            unitNode = massNode.transform.GetChild(0).gameObject;

                                            //移動しているユニットの情報を取得する
                                            string movingUnitCoordName = changeUnitCoord_Process_UnitGrabbedTrans.parent.gameObject.name;
                                            string movingUnitCoord_X = movingUnitCoordName.Substring(movingUnitCoordName.Length - 3)[0].ToString();

                                            unitNode.transform.parent = GameObject.Find("gt_" + movingUnitCoord_X + unitTypeSD_IsD + "3").transform;



                                            //移動しているユニットのとなりのyのステータスを確認（空 or ユニットの存在）
                                            if (int.Parse(movingUnitCoordName.Substring(movingUnitCoordName.Length - 1).ToString()) == 4)
                                                massNode = GameObject.Find("gt_" + movingUnitCoord_X + unitTypeSD_IsS + "3");
                                            else
                                                massNode = GameObject.Find("gt_" + movingUnitCoord_X + unitTypeSD_IsS + "4");

                                            //からじゃない場合は、ユニットの移動を行う
                                            if (massNode.transform.childCount > 0)
                                            {
                                                Transform unitNode02_transform = massNode.transform.GetChild(0);
                                                unitNode02_transform.parent = GameObject.Find("gt_" + int.Parse(finalcoord.x.ToString()) + unitTypeSD_IsS + "4").transform;
                                                unitNode02_transform.localPosition = new Vector3(0, 0, 0);
                                                unitNode02_transform.rotation = Quaternion.Euler(23, 0, 0);
                                            }

                                            //TypeSのユニットはカバーされるかもしれないので、動かす
                                            massNode = GameObject.Find("gt_" + movingUnitCoord_X + unitTypeSD_IsS + "3");

                                            if (massNode.transform.childCount > 0)
                                            {
                                                Transform unitNode02_transform = massNode.transform.GetChild(0).transform;
                                                unitNode02_transform.parent = GameObject.Find("gt_" + movingUnitCoord_X + unitTypeSD_IsS + "2").transform;
                                                unitNode02_transform.localPosition = new Vector3(0, 0, 0);
                                                unitNode02_transform.rotation = Quaternion.Euler(23, 0, 0);
                                            }
                                        }
                                        break;

                                    //S[x,4]
                                    case 4:
                                        //動かせるニットの情報を取得
                                        massNode = GameObject.Find("gt_" + int.Parse(finalcoord.x.ToString()) + unitTypeSD_IsD + "3");
                                        if (massNode.transform.childCount > 0)
                                        {
                                            //対象の座標にあるユニットの移動(階層の変更)
                                            unitNode = massNode.transform.GetChild(0).gameObject;

                                            //移動しているユニットの情報を取得する
                                            string movingUnitCoordName = changeUnitCoord_Process_UnitGrabbedTrans.parent.gameObject.name;
                                            string movingUnitCoord_X = movingUnitCoordName.Substring(movingUnitCoordName.Length - 3)[0].ToString();
                                            string movingUnitCoord_Y = movingUnitCoordName.Substring(movingUnitCoordName.Length - 1).ToString();

                                            //移動させる前に、隣のマスを確認する
                                            int limit_Move = 4;

                                            //TYPE D 移動しているユニットのとなりのyのステータスを確認（空 or ユニットの存在）
                                            massNode = GameObject.Find("gt_" + movingUnitCoord_X + unitTypeSD_IsD + "2");
                                            //空じゃない場合は、移動を中止
                                            if (massNode.transform.childCount > 0)
                                            {
                                                limit_Move = int.Parse(movingUnitCoord_X);
                                                finalcoord.x = limit_Move; //動いていたユニットの処理をリバースする
                                            }
                                            else
                                            {
                                                unitNode.transform.parent = GameObject.Find("gt_" + movingUnitCoord_X + unitTypeSD_IsD + "3").transform;
                                                //if (int.Parse(movingUnitCoord_X) != limit_Move)
                                                //{
                                                    //移動しているユニットのとなりのyのステータスを確認（空 or ユニットの存在）
                                                    if (int.Parse(movingUnitCoordName.Substring(movingUnitCoordName.Length - 1).ToString()) == 4)
                                                        massNode = GameObject.Find("gt_" + movingUnitCoord_X + unitTypeSD_IsS + "3");
                                                    else
                                                        massNode = GameObject.Find("gt_" + movingUnitCoord_X + unitTypeSD_IsS + "4");

                                                    //からじゃない場合は、ユニットの移動を行う
                                                    if (massNode.transform.childCount > 0)
                                                    {
                                                        Transform unitNode02_transform = massNode.transform.GetChild(0);
                                                        unitNode02_transform.parent = GameObject.Find("gt_" + int.Parse(finalcoord.x.ToString()) + unitTypeSD_IsS + "3").transform;
                                                        unitNode02_transform.localPosition = new Vector3(0, 0, 0);
                                                        unitNode02_transform.rotation = Quaternion.Euler(23, 0, 0);
                                                    }
                                                //}
                                            }
                                        }

                                        break;
                                }
                            }
                        }

                        #endregion

                        //移動しようとしているユニットの移動と回転を調整する
                        if (unitNode != null)
                        {
                            unitNode.transform.localPosition = new Vector3(0, 0, 0);
                            unitNode.transform.rotation = Quaternion.Euler(23, 0, 0);
                        }
                    }
                    // unitInDestinyMassCheck_Coordに対象の座標をコピーする（これで、対処の座標の変更の時だけに、ユニットの存在を確認する）
                    unitInDestinyMassCheck_Coord = finalcoord;
                }
            }
            #endregion


            //移動の処理を行う
            if (readytorender)
            {
                //移動しようとしているユニットのtransformをグリッドのfinalcoordに移動する
                if (GameObject.Find("gt_" + int.Parse(finalcoord.x.ToString()) + s_unitTypeSD + int.Parse(finalcoord.y.ToString())) != null)
                    changeUnitCoord_Process_UnitGrabbedTrans.transform.parent = GameObject.Find("gt_" + int.Parse(finalcoord.x.ToString()) + s_unitTypeSD + int.Parse(finalcoord.y.ToString())).transform;

                //移動しようとしているユニットの移動と回転を調整する
                changeUnitCoord_Process_UnitGrabbedTrans.transform.localPosition = new Vector3(0, 0, 0);
                changeUnitCoord_Process_UnitGrabbedTrans.transform.rotation = Quaternion.Euler(23, 0, 0);

                //HUDとUIの更新
                Update3DOverHUD();

                //ユニットの選択サイン
                charIndicator.transform.position = new Vector3(
                    changeUnitCoord_Process_UnitGrabbedTrans.position.x, 4.5f, changeUnitCoord_Process_UnitGrabbedTrans.position.z);

                //サウンドの再生
                if (unit_Move_LastPosition != finalcoord)
                    Play_Au_Sound_1038();


                unit_Move_LastPosition = finalcoord;
            }
        }

    }

    #endregion







    //システムの初期化
    void Start()
    {
        //FPSを20に設定する
        Application.targetFrameRate = 20;

        //解像度の設定
        SetScreenResolution();

        //ポインターをユニットを選択するモードにチェンジ
        SetPointerModeToSelectionMode();

        //チームモデルの配置
        InitTeamModels();

        //3DにオバーするUIの配置
        Init3DOverHUD();

        //ユニットの選択サイン
        charIndicator = GameObject.Find("charIndicator_ctrl");

}



    // Update is called once per frame
    void Update()
    {
        if(cutsceneIsPlaying)
        {
            PlayCutScene(cutsceneType);
            return;
        }


        //バトルの処理を行う
        if (battleActionNewOK != 0 && !processingActionFlag)    

        {
            processingActionFlag = true;     
            ProcessAttack(battleActionArray[1]);
        }

        if(battleActionNewOK != 0)
            performActionFlag = true;


        //チームのユニット移動の処理
        if (changeUnitCoord)
        {
            //移動の処理
            ChangeUnitCoord_Process(pointer_unit_initCoord, pointer_unit_finalCoord);
        }

        //UIの状況を確認する
        if (changeUnitCoord_Process_UnitGrabbedTrans != null)
        {
            int state = GameObject.Find("pu_" + changeUnitCoord_Process_UnitGrabbedTrans.gameObject.name).GetComponent<PlayerUnit>().state;
            if (state == 1)
            {
                GameObject.Find("ui_btn_cmd_01").GetComponent<Button>().interactable = true;
                GameObject.Find("ui_btn_cmd_02").GetComponent<Button>().interactable = true;
                GameObject.Find("ui_btn_cmd_03").GetComponent<Button>().interactable = true;
            }
        }


        if(!sceneInitialized)
        {
            //移動の処理を終わらせる
            EndUnitMoveMode(true);

            //キャラを選択するモードにチェンジ
            SetPointerModeToSelectionMode();

            sceneInitialized = true;
        }



    }


    //ポーズの処理
    //引数： b_pause(bool) (EnablePause/DisablePauseのリターン)
    //リターン： void
    private void OnApplicationPause(bool b_pause)
    {
        Debug.Log("ポーズ中");

        //スクリーンのスリープモードの設定
        SetScreenTimeout();

        GameObject.Find("hud_txt_pause").GetComponent<Image>().enabled = true;
        GameObject.Find("hud_dim_pause").GetComponent<Image>().enabled = true;
        //GameObject.Find("au_music_1001").GetComponent<AudioSource>().Pause();


    }


    //ポーズの解放の処理
    //引数： b_focus(bool) (EnablePause/DisablePauseのリターン)
    //リターン： void
    private void OnApplicationFocus(bool b_focus)
    {
        Debug.Log("ゲームの再開");
        //スクリーンのスリープモードの設定
        SetScreenTimeout();

        GameObject.Find("hud_txt_pause").GetComponent<Image>().enabled = false;
        GameObject.Find("hud_dim_pause").GetComponent<Image>().enabled = false;
        //GameObject.Find("au_music_1001").GetComponent<AudioSource>().Play();
    }




    //サウンド
    public void Play_Au_Sound_1037()
    {
        au_sound_1037.GetComponent<AudioSource>().Play();
    }

    public void Play_Au_Sound_1038()
    {
        au_sound_1038.GetComponent<AudioSource>().Play();
    }

    //サウンド
    public void Play_Au_Sound_Punch_01()
    {
        au_sound_punch_01.GetComponent<AudioSource>().Play();
    }

    public void Play_Au_Sound_Punch_02()
    {
        au_sound_punch_02.GetComponent<AudioSource>().Play();
    }


    //UIとHUD
    public void UpdateTeamUnit3DHUD(PlayerUnit myUnit)
    {

    }

    internal void UpdateTeamUnit2DHUD(GameObject go_selectedUnit)
    {

        PlayerUnit playerUnit = GameObject.Find("pu_" + go_selectedUnit.name).GetComponent<PlayerUnit>();
        string name = playerUnit.name;
        int lvl = playerUnit.lvl;
        int hp_pt = playerUnit.hp_pt;
        int current_hp_pt = playerUnit.hp_ptCurrent;
        float hud_hp = current_hp_pt/hp_pt;
        GameObject.Find("hud_char_name").GetComponent<Text>().text = name;
        GameObject.Find("hud_char_lvl").GetComponent<Text>().text = "Lv. " + lvl;
        GameObject.Find("hud_char_gauge_hp_top").GetComponent<Image>().fillAmount = hud_hp;

        int status = GameObject.Find("pu_" + go_selectedUnit.name).GetComponent<PlayerUnit>().state;

        if (status == 0)
        {
                GameObject.Find("ui_btn_cmd_01").GetComponent<Button>().interactable = false;
                GameObject.Find("ui_btn_cmd_02").GetComponent<Button>().interactable = false;
                GameObject.Find("ui_btn_cmd_03").GetComponent<Button>().interactable = false;
        }
        else if (status == 1)
        {
            GameObject.Find("ui_btn_cmd_01").GetComponent<Button>().interactable = true;
            GameObject.Find("ui_btn_cmd_02").GetComponent<Button>().interactable = true;
            GameObject.Find("ui_btn_cmd_03").GetComponent<Button>().interactable = true;
        }

        GameObject enemyIndicator = GameObject.Find("enemyIndicator_ctrl");
        switch(playerUnit.enemy)
        {
            case 1:
                enemyIndicator.transform.position = new Vector3(-4.1f, 4.5f, 17.3f);
                break;
            case 2:
                enemyIndicator.transform.position = new Vector3(4.1f, 4.5f, 17.3f);
                break;
            case 3:
                enemyIndicator.transform.position = new Vector3(0.0f, 4.5f, 24.3f);
                break;
            case 4:
                enemyIndicator.transform.position = new Vector3(4.1f, 4.5f, 24.3f);
                break;
            default:
                enemyIndicator.transform.position = new Vector3(1000f, 1000f, 1000f);
                break;

        }
    }


    //3Dモデルの初期化
    internal void InitTeamModels()
    {
        for (int i = 1; i < 7;i++)
        {
            string gameModel = "teamchar0" + i.ToString();
            GameObject model = GameObject.Find(gameModel);
            if (model != null)
            {
                if (model.transform.childCount > 0)
                {
                    string coord = null;
                    if (i < 5)
                    {
                        coord = "gt_3s" + i.ToString();
                        model.transform.parent = GameObject.Find(coord).transform;
                        model.transform.localPosition = new Vector3(0, 0, 0);
                        model.transform.rotation = Quaternion.Euler(23, 0, 0);
                    }

                    else
                    {
                        coord = "gt_4d";
                        if (i == 5)
                            coord = coord + "1";
                        else if (i == 6)
                            coord = coord + "3";

                        model.transform.parent = GameObject.Find(coord).transform;
                        model.transform.position = GameObject.Find(coord).transform.position;
                        model.transform.rotation = Quaternion.Euler(23, 0, 0);
                    }


                }
            }
        }
    }

    internal void Init3DOverHUD()
    {
        for (int i = 1; i < 7; i++)
        {
            string hudname = "ctrl_hud_teamchar0" + i.ToString();
            GameObject hudnode = GameObject.Find(hudname);
            if (hudnode != null)
            {
                if (hudnode.transform.childCount > 0)
                {
                    string coord = null;
                    if (i < 5)
                    {
                        coord = "hud_char_stat_gt_3s" + i.ToString();
                        hudnode.transform.parent = GameObject.Find(coord).transform;
                        hudnode.transform.localPosition = new Vector3(0, -14, 0);
                    }

                    else
                    {
                        coord = "hud_char_stat_gt_4d";
                        if (i == 5)
                            coord = coord + "1";
                        else if (i == 6)
                            coord = coord + "3";

                        hudnode.transform.parent = GameObject.Find(coord).transform;
                        hudnode.transform.localPosition = new Vector3(0, -14, 0);
                    }


                }
            }
        }
    }

    internal void Update3DOverHUD()
    {
        for (int i = 1; i < 7; i++)
        {
            string teamCharaName = "teamchar0";
            string hudName = "ctrl_hud_teamchar0";
            teamCharaName = teamCharaName + i.ToString();
            hudName = hudName + i.ToString();

            Debug.Log(teamCharaName);
            GameObject teamCharaLocation = GameObject.Find(teamCharaName).transform.parent.gameObject;
            string targetCoord = teamCharaLocation.name.Substring(teamCharaLocation.name.Length - 3).ToString();
            GameObject go_hud = GameObject.Find(hudName);
            GameObject go_target = GameObject.Find("hud_char_stat_gt_" + targetCoord);
            go_hud.transform.parent = go_target.transform;
            go_hud.transform.localPosition = new Vector3(0, -14, 0);
        }
    }

    internal void Update3DOverHUD_PerUnit_Load(string teamCharName, float currentLoad, int status)
    {
        string loadBarName = "gau_char0";
        loadBarName = loadBarName + teamCharName.Substring(teamCharName.Length - 1).ToString();
        loadBarName = loadBarName + "_load_top";
        GameObject.Find(loadBarName).GetComponent<Image>().fillAmount = currentLoad;

        string statusName = "txt_char0";
        statusName = statusName + teamCharName.Substring(teamCharName.Length - 1).ToString();
        statusName = statusName + "_status";
        if(status == 0)
        {
            GameObject.Find(statusName).GetComponent<Text>().text = "";
            GameObject.Find(loadBarName).GetComponent<Image>().color = new Vector4(1, 0.85f, 0.05f, 1);
        }

        else if (status == 1)
        {
            GameObject.Find(statusName).GetComponent<Text>().text = "READY!";
            //サウンドの再生 
                Play_Au_Sound_1037();
        }

    }

    //バトルシステム

    public void PerformAction(int action)
    {
        PlayerUnit pu_script = GameObject.Find("pu_" + go_selectedUnit.name).GetComponent<PlayerUnit>();
        pu_script.action = action;
        pu_script.state = 2;
        performActionFlag = true;
        GameObject.Find("ui_btn_cmd_01").GetComponent<Button>().interactable = false;
        GameObject.Find("ui_btn_cmd_02").GetComponent<Button>().interactable = false;
        GameObject.Find("ui_btn_cmd_03").GetComponent<Button>().interactable = false;

    }

    public void ProcessAttack(Attack myAttack)
    {

        Debug.Log("AttackFinished");
        if (myAttack.unit == 1)
            myAttack.pu.state = 3;
        if (myAttack.unit == 2)
            myAttack.eu.state = 3;
    }

    public Attack[] battleActionArray = new Attack [99];
    public int battleActionNewOK = 0;


    public void ProcessDamage(PlayerUnit pu_chara, int enemy)
    {
        int addScore = 0;
        //Score
        if (pu_chara.action == 1)
            addScore = 300;
        else if (pu_chara.action == 2)
            addScore = 900;


        float increment = addScore * (1 + ((float)pu_chara.pw_state / 100));





        int attack = 0;
        if (pu_chara.action == 1)
            attack = pu_chara.atk_pt;
        else if (pu_chara.action == 2)
            attack = (int)(1.25f * pu_chara.atk_pt);

        attack = (int)Random.Range((float)attack * 1.0f, ((float)attack * 1.15f));
        Debug.Log(attack);
        //Unidad que es atacada敵の選択のサイン
        EnemyUnit eu_enemy = GameObject.Find("pu_enemy0" + enemy.ToString()).GetComponent<EnemyUnit>();

        eu_enemy.hp_ptCurrent = eu_enemy.hp_ptCurrent - attack;

        if (eu_enemy.hp_ptCurrent < 0)
        {
            eu_enemy.hp_ptCurrent = 0;
            eu_enemy.isAlive = false;
            pu_chara.CheckandAutoSwitchEnemy();
            UpdateTeamUnit2DHUD(go_selectedUnit);
            cutsceneIsPlaying = true;
            GameObject.Find("cld_e" + enemy.ToString()).GetComponent<Image>().raycastTarget = false;
            increment = increment + 10000;
        }

        eu_enemy.UpdateHPHUD();

        score = score + (int)increment;
        if (increment != 0)
            GameObject.Find("hud_score_num").GetComponent<Text>().text = score.ToString();
    }


    internal bool cutsceneIsPlaying = false;
    internal int cutsceneType = 0;      // 1 = 死亡 3 = ゲーム終了WIN 4 = LOSE


    private void PlayCutScene(int cutsceneType)
    {
        PlayerUnit char01 = GameObject.Find("pu_teamchar01").GetComponent<PlayerUnit>();
        PlayerUnit char02 = GameObject.Find("pu_teamchar02").GetComponent<PlayerUnit>();
        PlayerUnit char03 = GameObject.Find("pu_teamchar03").GetComponent<PlayerUnit>();
        PlayerUnit char04 = GameObject.Find("pu_teamchar04").GetComponent<PlayerUnit>();
        PlayerUnit char05 = GameObject.Find("pu_teamchar05").GetComponent<PlayerUnit>();
        PlayerUnit char06 = GameObject.Find("pu_teamchar06").GetComponent<PlayerUnit>();

        EnemyUnit enemy01 = GameObject.Find("pu_enemy01").GetComponent<EnemyUnit>();
        EnemyUnit enemy02 = GameObject.Find("pu_enemy02").GetComponent<EnemyUnit>();
        EnemyUnit enemy03 = GameObject.Find("pu_enemy03").GetComponent<EnemyUnit>();

        if(
            !char01.isAlive
            &&
            !char02.isAlive
                        &&
            !char03.isAlive
                        &&
            !char04.isAlive
                        &&
            !char05.isAlive
                        &&
            !char06.isAlive
            )
        {
            GameObject.Find("ui_btn_pause").GetComponent<Image>().enabled = false;
            GameObject.Find("ui_btn_pause").GetComponent<Button>().enabled = false;
            GameObject.Find("hud_txt_pause_end").GetComponent<Text>().text = "YOU LOSE";
            
            EnablePausePlay();
        }

        if (!enemy01.isAlive
            &&
            !enemy02.isAlive
                        &&
            !enemy03.isAlive)
        {
            GameObject.Find("ui_btn_pause").GetComponent<Image>().enabled = false;
            GameObject.Find("ui_btn_pause").GetComponent<Button>().enabled = false;
            GameObject.Find("hud_txt_pause_end").GetComponent<Text>().text = "YOU WIN";
        }

        cutsceneIsPlaying = false;
        cutsceneType = 0;
    }



    public void SetEnemy(int enemy)
    {
        //敵の選択のサイン
        PlayerUnit pu_chara = GameObject.Find("pu_" + go_selectedUnit.name).GetComponent<PlayerUnit>();
        pu_chara.enemy = enemy;

        //ボードの上にあるHUDの更新（ユニット）
        UpdateTeamUnit2DHUD(go_selectedUnit);
    }


    public void ProcessDamageToPlayer(EnemyUnit eu_enemy, int player)
    {
        int attack = 0;
        if (eu_enemy.action == 1)
            attack = eu_enemy.atk_pt;
        else if (eu_enemy.action == 2)
            attack = (int)(1.25f * eu_enemy.atk_pt);

        attack = (int)Random.Range((float)attack * 1.0f, ((float)attack * 1.15f));
        Debug.Log(attack);
        //Unidad que es atacada敵の選択のサイン
        PlayerUnit pu_player = GameObject.Find("pu_teamchar0" + player.ToString()).GetComponent<PlayerUnit>();

        pu_player.hp_ptCurrent = pu_player.hp_ptCurrent - attack;

        if (pu_player.hp_ptCurrent < 0)
        {
            pu_player.hp_ptCurrent = 0;
            pu_player.isAlive = false;
            eu_enemy.CheckandAutoSwitchEnemy();
            if(go_selectedUnit.name.Substring(go_selectedUnit.name.Length -1) == player.ToString())
                UpdateTeamUnit2DHUD(go_selectedUnit);
            cutsceneIsPlaying = true;
        }

    }














    internal int score = 0;
}





public class Attack
{
    public PlayerUnit pu;
    public int unit;
    public int type;
    public int enemy;
    public EnemyUnit eu;
}


