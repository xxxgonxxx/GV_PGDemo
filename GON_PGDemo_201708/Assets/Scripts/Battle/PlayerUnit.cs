using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlayerUnit : MonoBehaviour {


    //以下の情報はサーバーから取るはず (現時点ではローカルで持たせている)
    internal string name = "null";	//名前
    internal int rarity = 0; // 0 = 基本、 1 = レア、 2 = スーパー、 3 = ウルトラ
    internal int lvl = 0;	//LEVEL
    internal int atk_pt = 0;	//ATTACK
    internal int dfs_pt = 0;    //DEFENSE
    internal int hp_pt = 1; //HP
    internal int hp_ptCurrent = 1; //HP

    internal int ts_pt = 00;    //チームスキルのげーじゅへの貢献度

    internal int pw_state = 0;	//パワーのステータス。0～2 (POWER UPコマンドで上がる）
    internal int pw_up = 0; // パワーアップの%
    internal int state = 0; //0 = スタンドバイ、 1= コマンド入力待ち中、2=待ち中、3=アクション中


    //ロード中の情報
    internal float loadbarTime = 0;
    internal float loadbarStartTime = 0;
    internal float loadbarCurrent = 0; //HP

    internal BattleSceneManager battleSceneManager;


    internal int enemy = 1;
    internal bool playerInitialized = false;
    internal int playerInitBuffer = 0;

    //myAttack
    internal Attack myAttack = null;


    internal int action = 0;    // 0 = ない、1=攻撃、2=Elemental、3=パワーアップ、4=TS
                                // Use this for initialization
    void Start ()

    {
        //バトルマネージャのハンドル
        battleSceneManager = GameObject.Find("BattleSceneManager").GetComponent<BattleSceneManager>();


        rarity = Random.Range(2, 3);
        switch (rarity)
        {
            case 0:
                lvl = Random.Range(8, 20);
                break;
            case 1:
                lvl = Random.Range(21, 40);
                break;
            case 2:
                lvl = Random.Range(41, 60);
                break;
                lvl = Random.Range(71, 90);
            case 3:
                lvl = Random.Range(91, 100);
                break;
        }


        Animation myAnim = this.gameObject.GetComponent<Animation>();
        myAnim["Stats"].time = lvl;
        myAnim.Play();



        //仮の初期化（サーバからデータを取得するべき）
        name = this.gameObject.name;	//名前


        ts_pt = 1;    //チームスキルのゲージのへの貢献度

        pw_up = 0;
        state = 0; //0 = スタンドバイ、 1= コマンド入力待ち中、2=待ち中、3=アクション中


        loadbarTime = Mathf.Lerp(3.5f, 2.2f, ((float)lvl/100));
        int loadbarRandomStartTime = Random.Range(30, 85);
        loadbarStartTime = (loadbarTime * loadbarRandomStartTime)/(-100);
        loadbarCurrent = (float)loadbarRandomStartTime / 100;

        //仮の敵
        enemy = 1;


    }

    // Update is called once per frame
    void Update ()

    {

        if (!playerInitialized && playerInitBuffer == 1)
        {

            hp_pt = (int)transform.position.x;
            atk_pt = (int)transform.position.y;
            dfs_pt = (int)transform.position.z;
            playerInitialized = true;

            Debug.Log("HP " + hp_pt);
            Debug.Log("ATK " + atk_pt);
            Debug.Log("DFS " + dfs_pt);
            hp_ptCurrent = hp_pt;
            GameObject hp = GameObject.Find(this.gameObject.name + "_hud_HP");
            float hud_hp = hp_ptCurrent / hp_pt;
            GameObject.Find("hud_char_gauge_hp_top").GetComponent<Image>().fillAmount = hud_hp;
        }


        if (playerInitBuffer < 1)
            playerInitBuffer++;




        //スタンドバイ
        if (state == 0)
        {
            //ロードバーの開始時間を取得する
            if(loadbarStartTime == -1)
            {
                loadbarStartTime = Time.time;
                myAttack = null;
                animationSecondPartFinished = false;
                animationFirstPartFinished = false;
                animationInitialized = false;
                if (action != 3)
                    pw_state = 0;
            }
            //ロードの状況を確認してUIの更新を行う
            loadbarCurrent = (Time.time - loadbarStartTime) / loadbarTime;

            //次のステータスへのフラグ
            if (Time.time > loadbarStartTime + loadbarTime)
            {
                state = 1;
            }
            //UIの情報の更新
            battleSceneManager.Update3DOverHUD_PerUnit_Load(this.gameObject.name, loadbarCurrent, state);
        }
        //コマンド入力待ち中
        else if (state == 1)
        {
            loadbarStartTime = -1;  //スタンドバイでロードバーの開始時間を取得するためのリセット

            //アクションをQueueに

            //state = 2;
        }

        //2=待ち中
        else if (state == 2)
        {
            if (myAttack ==null)
            {
                battleSceneManager.battleActionNewOK++;
                myAttack = new Attack();
                myAttack.pu = this.gameObject.GetComponent<PlayerUnit>();
                myAttack.unit = 1;
                myAttack.type = action;
                CheckandAutoSwitchEnemy();
                myAttack.enemy = enemy;
                Debug.Log(battleSceneManager.battleActionNewOK);

                battleSceneManager.battleActionArray[battleSceneManager.battleActionNewOK] = myAttack;
            }
        }
        //アクション中
        else if (state == 3)
        {
            //Check if enemy is alive

            if (myAttack.type != 3 && isAlive)
            {
                //Animation A
                if (!animationInitialized)
                {
                    animationInitialized = true;
                    string splineInit = null;
                    if (enemy == 1)
                    {
                        MySpline = GameObject.Find("ESpline01").GetComponent<Spline>();
                        splineInit = "e1";
                    }
                    else if (enemy == 2)
                    {
                        MySpline = GameObject.Find("ESpline02").GetComponent<Spline>();
                        splineInit = "e2";
                    }

                    else if (enemy == 3)
                    {
                        MySpline = GameObject.Find("ESpline03").GetComponent<Spline>();
                        splineInit = "e3";
                    }

                    thisPlayer = GameObject.Find(this.gameObject.name.Substring(this.gameObject.name.Length - 10));

                    GameObject splineCoord00 = GameObject.Find(splineInit + "0000");
                    GameObject splineCoord01 = GameObject.Find(splineInit + "0001");

                    string prentNode = thisPlayer.transform.parent.gameObject.name;

                    splineCoord00.transform.position = thisPlayer.transform.position;
                    if (prentNode.Substring(prentNode.Length - 3)[0].ToString() == "2" || prentNode.Substring(prentNode.Length - 3)[0].ToString() == "1")
                        splineCoord01.transform.position = new Vector3(splineCoord00.transform.position.x - 0.5f, splineCoord00.transform.position.y, splineCoord00.transform.position.z + 1.5f);
                    else
                        splineCoord01.transform.position = new Vector3(splineCoord00.transform.position.x, splineCoord00.transform.position.y, splineCoord00.transform.position.z - 1.5f);

                    thisPlayer.transform.position = MySpline.GetPositionOnSpline(Mathf.Clamp(0, 0f, 1.0f));
                    framescounter = 1;
                    frames = 12;
                }

                if (framescounter < frames && !animationFirstPartFinished)
                {
                    float increment = ((float)framescounter / frames);

                    if (framescounter == 1)
                        increment = 0.13f;
                    if (framescounter == 2)
                        increment = 0.1f;
                    if (framescounter == 3)
                        ;
                    increment = 0.08f;

                    if (framescounter == 4)
                    {
                        battleSceneManager.Play_Au_Sound_Punch_02();
                        increment = 0.13f;
                    }

                    if (framescounter == 5)
                    {
                        battleSceneManager.Play_Au_Sound_Punch_02();
                        increment = 0.35f;
                    }
                    if (framescounter == 6)


                        increment = 0.65f;


                    if (framescounter == 7)
                    {
                        battleSceneManager.Play_Au_Sound_Punch_01();
                        increment = 0.95f;
                    }

                    if (framescounter == 8)
                        increment = 0.96f;
                    if (framescounter == 9)
                        increment = 0.97f;
                    if (framescounter == 10)
                        increment = 0.98f;
                    if (framescounter == 11)
                        increment = 0.99f;
                    if (framescounter == 12)
                        increment = 1.0f;

                    thisPlayer.transform.position = MySpline.GetPositionOnSpline(Mathf.Clamp(increment, 0f, 1.0f));
                    framescounter++;
                }
                if (framescounter == frames)
                {
                    animationFirstPartFinished = true;
                    if (pw_state != 0)
                        atk_pt = atk_pt * (1 + pw_state);
                    battleSceneManager.ProcessDamage(this.gameObject.GetComponent<PlayerUnit>(), enemy);
                }
                if (framescounter <= frames && animationFirstPartFinished && !animationSecondPartFinished)
                {
                    if (framescounter == 0)
                    {
                        ; animationSecondPartFinished = true;
                    }
                    float increment = ((float)framescounter / frames);
                    if (framescounter == 1)
                        increment = 0.0f;
                    if (framescounter == 2)
                        increment = 0.05f;
                    if (framescounter == 3)
                        increment = 0.1f;
                    if (framescounter == 4)
                        increment = 0.25f;
                    if (framescounter == 5)
                        increment = 0.45f;
                    if (framescounter == 6)
                        increment = 0.75f;
                    if (framescounter == 7)
                        increment = 0.95f;

                    if (framescounter == 8)
                        increment = 0.96f;
                    if (framescounter == 9)
                        increment = 0.97f;
                    if (framescounter == 10)
                        increment = 0.98f;
                    if (framescounter == 11)
                        increment = 0.99f;
                    if (framescounter == 12)
                        increment = 1.0f;

                    thisPlayer.transform.position = MySpline.GetPositionOnSpline(Mathf.Clamp(increment, 0f, 1.0f));
                    framescounter--;
                }

                if (animationSecondPartFinished)
                {
                    thisPlayer.transform.localPosition = new Vector3(0.0f, 0.0f, 0.0f);
                    bool stopCycle = false;
                    for (int i = 0; i < battleSceneManager.battleActionArray.Length; i++)
                    {
                        if (i != 0)
                        {
                            if (battleSceneManager.battleActionArray[i + 1] != null)
                                battleSceneManager.battleActionArray[i] = battleSceneManager.battleActionArray[i + 1];
                            else
                                stopCycle = true;
                        }
                        if (stopCycle)
                            break;
                    }
                    battleSceneManager.battleActionNewOK--;
                    battleSceneManager.processingActionFlag = false;
                    battleSceneManager.performActionFlag = false;

                    state = 0;
                }
            }
            else if (myAttack.type == 3 && isAlive)
            {
                if (!animationInitialized)
                {
                    timeofAnimStart = Time.time;
                    animationInitialized = true;
                }
                else
                {
                    if (Time.time > timeofAnimStart + timeOfAnim)
                    {
                        bool stopCycle = false;
                        for (int i = 0; i < battleSceneManager.battleActionArray.Length; i++)
                        {
                            if (i != 0)
                            {
                                if (battleSceneManager.battleActionArray[i + 1] != null)
                                    battleSceneManager.battleActionArray[i] = battleSceneManager.battleActionArray[i + 1];
                                else
                                    stopCycle = true;
                            }
                            if (stopCycle)
                                break;
                        }

                        battleSceneManager.battleActionNewOK--;
                        battleSceneManager.processingActionFlag = false;
                        battleSceneManager.performActionFlag = false;
                        pw_state++;
                        state = 0;
                    }
                }
            }
            else if (!isAlive)
            {
                bool stopCycle = false;
                for (int i = 0; i < battleSceneManager.battleActionArray.Length; i++)
                {
                    if (i != 0)
                    {
                        if (battleSceneManager.battleActionArray[i + 1] != null)
                            battleSceneManager.battleActionArray[i] = battleSceneManager.battleActionArray[i + 1];
                        else
                            stopCycle = true;
                    }
                    if (stopCycle)
                        break;


                    battleSceneManager.battleActionNewOK--;
                    battleSceneManager.processingActionFlag = false;
                    battleSceneManager.performActionFlag = false;

                    state = 99;
                }
            }
        }

    }

    bool animationInitialized = false;
    bool animationFirstPartFinished = false;
    bool animationSecondPartFinished = false;

    int frames;
    int framescounter;
    GameObject thisPlayer;
    Spline MySpline;
    float timeOfAnim = 1.0f;
        float timeofAnimStart = 0.0f;

    internal bool isAlive = true;

    internal void CheckandAutoSwitchEnemy()
    {
        EnemyUnit eu_enemy = GameObject.Find("pu_enemy0" + enemy.ToString()).GetComponent<EnemyUnit>();
        if (eu_enemy.isAlive == false)
        {
            if (enemy == 1)
            {
                eu_enemy = GameObject.Find("pu_enemy02").GetComponent<EnemyUnit>();
                if (eu_enemy.isAlive == true)
                    enemy = 2;
                else
                    enemy = 3;
            }
            else if (enemy == 2)
            {
                eu_enemy = GameObject.Find("pu_enemy01").GetComponent<EnemyUnit>();
                if (eu_enemy.isAlive == true)
                    enemy = 1;
                else
                    enemy = 3;
            }
            else if (enemy == 3)
            {
                eu_enemy = GameObject.Find("pu_enemy01").GetComponent<EnemyUnit>();
                if (eu_enemy.isAlive == true)
                    enemy = 1;
                else
                    enemy = 2;
            }

        }
    }
}
