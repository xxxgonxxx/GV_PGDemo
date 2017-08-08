using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;     //イベントシステムのハンドラー

//タップ関連の情報を認識するようにハンドラーを追加
public class ui3dEventHandler : MonoBehaviour, 
    IPointerUpHandler, 
    IPointerDownHandler, 
    IPointerClickHandler,
    IPointerEnterHandler, 
    IPointerExitHandler
{
    //public int mouseOnCount = 0;

    //ポインターのモード
    /*private enum PointerMode
    {
        UnitSelectionMode,                //キャラを選択するモード
        UnitMoveMode,                     //キャラを移動するモード
        NUM
    }*/

    //ポインターのハンドル
    private GameObject pointerHandler;                          //GameObject
    private string pointerHandler_name = "BattleSceneManager";  //名前
    private BattleSceneManager battleSceneManager;              //クラス
    private BattleSceneManager.PointerMode pointerMode;                            //このクラスのハンドラー
    private string pointerHandler_LoadErrorMessage = "BattleSceneManagerからpointerModeを取得できなかった。";   //ロードの失敗のメッセージ


    private Vector2 coord = new Vector2(0, 0);        //バトルシーンマネージャに送信する座標



    //バトルシーンマネージャからのポインターの取得の失敗
    private void GetPointerMode_LoadError()
    {
        Debug.LogError(pointerHandler_LoadErrorMessage);
    }
    //バトルシーンマネージャからのポインターの取得
    private void GetPointerMode()
    {
        if(pointerHandler==null)
            pointerHandler = GameObject.Find(pointerHandler_name);
        if (pointerHandler != null)
        {
            if (battleSceneManager == null)
                battleSceneManager = pointerHandler.GetComponent<BattleSceneManager>();
        }
        if (battleSceneManager != null)
            pointerMode = battleSceneManager.pointerMode;
        else
            GetPointerMode_LoadError();
    }

    //初期化：バトルシーンマネージャからのポインターの取得
    void Start()
    {
        //バトルシーンマネージャからのポインターの取得
        GetPointerMode();

        //キャラを選択するモード
        if (pointerMode == BattleSceneManager.PointerMode.UnitSelectionMode)
        {
            Debug.Log("You are selecting a unit.");

            //キャラを移動するモードにチェンジ
            battleSceneManager.SetPointerModeToMoveMode();

            //キャラをキープする：バトルシーンマネージャに選択したキャラの座標を送信
            coord = new Vector2
                (3,1);

            //座標の送信と移動処理のフラグ
            battleSceneManager.SetPointerUnitOriginalCoord(coord, true);
        }
    }

    //ポインターのタップのアクション：キャラをキープした
    public void OnPointerDown(PointerEventData eventData)
    {
        if (this.name.Substring(this.name.Length - 2)[0].ToString() == "e")
        {
            return;
        }

        //バトルシーンマネージャからのポインターの取得
        GetPointerMode();

        //キャラを選択するモード
        if (pointerMode == BattleSceneManager.PointerMode.UnitSelectionMode)
        {
            Debug.Log("You are selecting a unit.");

            //キャラを移動するモードにチェンジ
            battleSceneManager.SetPointerModeToMoveMode();

            //キャラをキープする：バトルシーンマネージャに選択したキャラの座標を送信
            coord = new Vector2
                (
                int.Parse(this.name.Substring(this.name.Length - 3)[0].ToString()), 
                int.Parse(this.name.Substring(this.name.Length - 1))
                );

            //座標の送信と移動処理のフラグ
            battleSceneManager.SetPointerUnitOriginalCoord(coord, true);
        }

    }

    //ポインターのタップのアクション（同じエリア）：キャラを移動せず、解除した
    public void OnPointerUp(PointerEventData eventData)
    {
        if (this.name.Substring(this.name.Length - 2)[0].ToString() == "e")
        {
            return;
        }

        //バトルシーンマネージャからのポインターの取得
        GetPointerMode();

        //キャラを移動するモード
        if (pointerMode == BattleSceneManager.PointerMode.UnitMoveMode)
        {
            //移動の変更があれば、キャラを移動する
            Debug.Log("You are trying to drop a unit (may be touching a same place, valid zone, forbidden zone).");

            //キャラを移動する：バトルシーンマネージャに決まった位置の座標を送信
            coord = new Vector2
                (
                int.Parse(this.name.Substring(this.name.Length - 3)[0].ToString()),
                int.Parse(this.name.Substring(this.name.Length - 1))
                );
            //移動の処理を終わらせる
            battleSceneManager.EndUnitMoveMode(true);

            //キャラを選択するモードにチェンジ
            battleSceneManager.SetPointerModeToSelectionMode();
        }
    }

    //ポインターのタップのときのアクション：キャラの移動がなかった
    public void OnPointerClick(PointerEventData eventData)
    {
        if (this.name.Substring(this.name.Length - 2)[0].ToString() == "e")
        {
            return;
        }

        //バトルシーンマネージャからのポインターの取得
        GetPointerMode();

        //キャラを移動するモード⇒キャラを選択するモード(OnPointerUpの処理)
        //  結果として・・・↓
        //キャラを選択するモード
        if (pointerMode == BattleSceneManager.PointerMode.UnitSelectionMode)
        {
            //移動の変更があれば、キャラを移動する
            Debug.Log("You are not moving the unit from its place.");
        }
    }


    //ポインターを認識したときのアクション
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (this.name.Substring(this.name.Length - 2)[0].ToString() == "e")
        {
            return;
        }
        //バトルシーンマネージャからのポインターの取得
        GetPointerMode();

        //Debug.Log("ポインターはHoverをはじめた");

        //キャラを移動するモード：キャラを移動しようとしている
        if (pointerMode == BattleSceneManager.PointerMode.UnitMoveMode)
        {
            Debug.Log("You started checking if you want to drop a unit here.");

            //キャラを移動しようとしている：バトルシーンマネージャに検討中の位置の座標を送信 
            coord = new Vector2
                (
                int.Parse(this.name.Substring(this.name.Length - 3)[0].ToString()),
                int.Parse(this.name.Substring(this.name.Length - 1))
                );
            //座標の送信と移動処理のフラグ
            battleSceneManager.SetPointerUnitCheckingCoord(coord, true);
        }
    }

    //ポインターの認識が終了になったときのアクション
    public void OnPointerExit(PointerEventData eventData)
    {
        if (this.name.Substring(this.name.Length - 2)[0].ToString() == "e")
        {
            return;
        }
        //バトルシーンマネージャからのポインターの取得
        GetPointerMode();

        //Debug.Log("ポインターはHoverをやめた");

        //キャラを移動するモード：キャラを移動する
        if (pointerMode == BattleSceneManager.PointerMode.UnitMoveMode)
        {
            Debug.Log("You didn't wanted to put your unit there.");

            //キャラを移動しようとしている：バトルシーンマネージャに検討中の位置の座標を送信 
            coord = new Vector2
                (
                int.Parse(this.name.Substring(this.name.Length - 3)[0].ToString()),
                int.Parse(this.name.Substring(this.name.Length - 1))
                );
            //座標の送信と移動処理のフラグ
            //battleSceneManager.SetPointerUnitCheckingCoord(coord, true);

        }
    }

}
