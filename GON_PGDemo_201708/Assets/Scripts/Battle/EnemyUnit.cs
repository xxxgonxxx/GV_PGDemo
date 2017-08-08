using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class EnemyUnit : MonoBehaviour {

    internal int lvl;
    internal int rarity;
    internal int hp_ptCurrent;
    internal int hp_pt;
    internal int atk_pt;
    internal int dfs_pt;
    internal bool isAlive = true;
    internal bool enemyInitialized = false;
    internal int enemyInitBuffer = 0;


    internal float loadbarStartTime = 0;
    internal float loadbarCurrent = 0;
    internal float loadbarTime = 0;
    internal BattleSceneManager battleSceneManager;
    internal bool animationSecondPartFinished = false;
    internal bool animationFirstPartFinished = false;
    internal bool animationInitialized = false;
    internal Attack myAttack = null;
    internal Spline MySpline;
    internal GameObject thisPlayer;

    internal int framescounter = 0;
    internal int frames = 0;
    internal int enemy = 0;
    internal int action = 0;    //いつも攻撃 1

    internal int state = 0;
    // Use this for initialization
    void Start() {
        ;
        rarity = Random.Range(1, 2);
        switch (rarity)
        {
            case 0:
                lvl = Random.Range(8, 16);
                break;
            case 1:
                lvl = Random.Range(17, 30);
                break;
            case 2:
                lvl = Random.Range(31, 60);
                break;
                lvl = Random.Range(71, 90);
            case 3:
                lvl = Random.Range(91, 100);
                break;
        }
        Animation myAnim = this.gameObject.GetComponent<Animation>();
        myAnim["Stats"].time = lvl;
        myAnim.Play();


        //その他のパラメータ


        loadbarTime = Mathf.Lerp(3.5f, 2.2f, ((float)lvl / 100)) * 10;
        int loadbarRandomStartTime = Random.Range(10, 20);
        loadbarStartTime = (loadbarTime * loadbarRandomStartTime) / (-100);
        loadbarCurrent = (float)loadbarRandomStartTime / 100;

        battleSceneManager = GameObject.Find("BattleSceneManager").GetComponent<BattleSceneManager>();

        framescounter = 0;
        frames = 0;
        enemy = 1;
        action = 1;    //いつも攻撃 1

    }


    internal void UpdateHPHUD()
    {
        GameObject hp = GameObject.Find(this.gameObject.name + "_hud_HP");
        hp.GetComponent<Text>().text = "HP: " + hp_ptCurrent.ToString() + " / " + hp_pt.ToString();
    }

    // Update is called once per frame
    void Update()
    {

        if (!enemyInitialized && enemyInitBuffer == 1)
        {

            hp_pt = (int)transform.position.x;
            atk_pt = (int)transform.position.y;
            dfs_pt = (int)transform.position.z;
            enemyInitialized = true;

            Debug.Log("HP " + hp_pt);
            Debug.Log("ATK " + atk_pt);
            Debug.Log("DFS " + dfs_pt);
            hp_ptCurrent = hp_pt;
            GameObject hp = GameObject.Find(this.gameObject.name + "_hud_HP");
            hp.GetComponent<Text>().text = "HP: " + hp_ptCurrent.ToString() + " / " + hp_pt.ToString();
            hp.GetComponent<Text>().enabled = true;
        }


        if (enemyInitBuffer < 1)
            enemyInitBuffer++;


        //攻撃

        //スタンドバイ
        if (state == 0)
        {
            //ロードバーの開始時間を取得する
            if (loadbarStartTime == -1)
            {
                loadbarStartTime = Time.time;
                myAttack = null;
                animationSecondPartFinished = false;
                animationFirstPartFinished = false;
                animationInitialized = false;/*
                if (action != 3)
                    pw_state = 0;*/
            }
            //ロードの状況を確認してUIの更新を行う
            loadbarCurrent = (Time.time - loadbarStartTime) / loadbarTime;

            //次のステータスへのフラグ
            if (Time.time > loadbarStartTime + loadbarTime)
            {
                state = 1;
            }
            //UIの情報の更新
            //battleSceneManager.Update3DOverHUD_PerUnit_Load(this.gameObject.name, loadbarCurrent, state);
        }
        //コマンド入力待ち中
        else if (state == 1)
        {
            loadbarStartTime = -1;  //スタンドバイでロードバーの開始時間を取得するためのリセット

            //アクションをQueueに

            state = 2;
        }

        //2=待ち中
        else if (state == 2)
        {
            if (myAttack == null)
            {
                battleSceneManager.battleActionNewOK++;
                myAttack = new Attack();
                myAttack.eu = this.gameObject.GetComponent<EnemyUnit>();
                myAttack.unit = 2;
                myAttack.type = action;

                CheckandAutoSwitchEnemy();
                myAttack.enemy = enemy;
                battleSceneManager.battleActionArray[battleSceneManager.battleActionNewOK] = myAttack;
            }
        }
        //アクション中
        else if (state == 3)
        { Debug.Log("ENTRE AQUI");
            //Check if enemy is alive

            if (isAlive == true)
            {
                //Animation A
                if (!animationInitialized)
                {
                    animationInitialized = true;

                    string lastLetter = this.gameObject.name.Substring(this.gameObject.name.Length - 1);
                    int li_lastLetter = int.Parse(lastLetter);

                    thisPlayer = GameObject.Find("enemy_" + this.gameObject.name.Substring(this.gameObject.name.Length - 2));

                    Debug.Log(this.gameObject.name.Substring(this.gameObject.name.Length - 7));
                    if (li_lastLetter == 1)
                    {
                        MySpline = GameObject.Find("ETPSpline01").GetComponent<Spline>();
                    }
                    else if (li_lastLetter == 2)
                    {
                        MySpline = GameObject.Find("ETPSpline02").GetComponent<Spline>();
                    }

                    else if (li_lastLetter == 3)
                    {
                        MySpline = GameObject.Find("ETPSpline03").GetComponent<Spline>();

                    }

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
                    battleSceneManager.ProcessDamageToPlayer(this.gameObject.GetComponent<EnemyUnit>(), enemy);
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
            else
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





    internal void CheckandAutoSwitchEnemy()
    {
        PlayerUnit pu_player = GameObject.Find("pu_teamchar0" + enemy.ToString()).GetComponent<PlayerUnit>();
        if (pu_player.isAlive == false)
        {
            if (enemy == 1)
            {
                pu_player = GameObject.Find("pu_teamchar02").GetComponent<PlayerUnit>();
                if (pu_player.isAlive == true)
                    enemy = 2;
                else
                {
                    pu_player = GameObject.Find("pu_teamchar03").GetComponent<PlayerUnit>();
                    if (pu_player.isAlive == true)
                        enemy = 3;
                    else
                    {
                        pu_player = GameObject.Find("pu_teamchar04").GetComponent<PlayerUnit>();
                        if (pu_player.isAlive == true)
                            enemy = 4;
                        else
                        {
                            pu_player = GameObject.Find("pu_teamchar05").GetComponent<PlayerUnit>();
                            if (pu_player.isAlive == true)
                                enemy = 5;
                            else
                                enemy = 6;

                        }

                    }
                }
            }
            else if (enemy == 2)
            {
                pu_player = GameObject.Find("pu_teamchar01").GetComponent<PlayerUnit>();
                if (pu_player.isAlive == true)
                    enemy = 1;
                else
                {
                    pu_player = GameObject.Find("pu_teamchar03").GetComponent<PlayerUnit>();
                    if (pu_player.isAlive == true)
                        enemy = 3;
                    else
                    {
                        pu_player = GameObject.Find("pu_teamchar04").GetComponent<PlayerUnit>();
                        if (pu_player.isAlive == true)
                            enemy = 4;
                        else
                        {
                            pu_player = GameObject.Find("pu_teamchar05").GetComponent<PlayerUnit>();
                            if (pu_player.isAlive == true)
                                enemy = 5;
                            else
                                enemy = 6;

                        }

                    }
                }
            }
            else if (enemy == 3)
            {
                pu_player = GameObject.Find("pu_teamchar02").GetComponent<PlayerUnit>();
                if (pu_player.isAlive == true)
                    enemy = 2;
                else
                {
                    pu_player = GameObject.Find("pu_teamchar01").GetComponent<PlayerUnit>();
                    if (pu_player.isAlive == true)
                        enemy = 1;
                    else
                    {
                        pu_player = GameObject.Find("pu_teamchar04").GetComponent<PlayerUnit>();
                        if (pu_player.isAlive == true)
                            enemy = 4;
                        else
                        {
                            pu_player = GameObject.Find("pu_teamchar05").GetComponent<PlayerUnit>();
                            if (pu_player.isAlive == true)
                                enemy = 5;
                            else
                                enemy = 6;
                        }

                    }
                }
            }


            else if (enemy == 4)
            {
                pu_player = GameObject.Find("pu_teamchar02").GetComponent<PlayerUnit>();
                if (pu_player.isAlive == true)
                    enemy = 2;
                else
                {
                    pu_player = GameObject.Find("pu_teamchar01").GetComponent<PlayerUnit>();
                    if (pu_player.isAlive == true)
                        enemy = 1;
                    else
                    {
                        pu_player = GameObject.Find("pu_teamchar03").GetComponent<PlayerUnit>();
                        if (pu_player.isAlive == true)
                            enemy = 3;
                        else
                        {
                            pu_player = GameObject.Find("pu_teamchar05").GetComponent<PlayerUnit>();
                            if (pu_player.isAlive == true)
                                enemy = 5;
                            else
                                enemy = 6;
                        }

                    }
                }
            }

            else if (enemy == 5)
            {
                pu_player = GameObject.Find("pu_teamchar02").GetComponent<PlayerUnit>();
                if (pu_player.isAlive == true)
                    enemy = 2;
                else
                {
                    pu_player = GameObject.Find("pu_teamchar01").GetComponent<PlayerUnit>();
                    if (pu_player.isAlive == true)
                        enemy = 1;
                    else
                    {
                        pu_player = GameObject.Find("pu_teamchar03").GetComponent<PlayerUnit>();
                        if (pu_player.isAlive == true)
                            enemy = 3;
                        else
                        {
                            pu_player = GameObject.Find("pu_teamchar04").GetComponent<PlayerUnit>();
                            if (pu_player.isAlive == true)
                                enemy = 4;
                            else
                                enemy = 6;
                        }

                    }
                }
            }

            else if (enemy == 6)
            {
                pu_player = GameObject.Find("pu_teamchar02").GetComponent<PlayerUnit>();
                if (pu_player.isAlive == true)
                    enemy = 2;
                else
                {
                    pu_player = GameObject.Find("pu_teamchar01").GetComponent<PlayerUnit>();
                    if (pu_player.isAlive == true)
                        enemy = 1;
                    else
                    {
                        pu_player = GameObject.Find("pu_teamchar03").GetComponent<PlayerUnit>();
                        if (pu_player.isAlive == true)
                            enemy = 3;
                        else
                        {
                            pu_player = GameObject.Find("pu_teamchar04").GetComponent<PlayerUnit>();
                            if (pu_player.isAlive == true)
                                enemy = 4;
                            else
                                enemy = 5;
                        }

                    }
                }
            }
        }
    }
}
