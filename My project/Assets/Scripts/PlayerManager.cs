using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using TMPro;
using UnityEngine.UI;

public class PlayerManager : MonoBehaviourPunCallbacks
{
    public static PlayerManager myPlayerManager = null;
    public static PlayerManager enemyPlayerManager = null;
    private void Awake() {
        if(myPlayerManager == null)
        {
            myPlayerManager = this;
        }
        else
        {
            enemyPlayerManager = this;
            if(PhotonNetwork.IsMasterClient) GameManager.instance.coinToss();
        }
    }

    public List<CardData> cardDataBuffer;
    public cardDataSO cardDataSO;
    public GameObject cardPrefab;
    public PhotonView PV;

    public List<Card> myCards;
    public List<GameObject> myCardsGameObj;
    public Vector3 myCardsLeft;
    public Vector3 myCardsRight;

    private void Start() {
        SetupItemBuffer();
        transform.position = new Vector3(-2,-3.8f);
        if(PV.IsMine)
        {
            transform.position = new Vector3(-2,-3.8f,0);
            myCardsLeft = new Vector3(-0.5f,-4.2f,0);
            myCardsRight = new Vector3(2.24f,-4.2f,0);
        }
        else
        {
            transform.position = new Vector3(-2,3.8f,0);
            myCardsLeft = new Vector3(-0.5f,4.2f,0);
            myCardsRight = new Vector3(2.24f,4.2f,0);

        }

        AddFiveCard();
    }

    private void Update() {
        if(Input.GetKeyDown(KeyCode.D) && PV.IsMine)
        {
            PV.RPC("destroyCard", RpcTarget.AllBuffered, 0);
        }
    }

    
    public void destroyMe(int index)
    {
        PV.RPC("destroyCard", RpcTarget.AllBuffered, index);
    }

    [PunRPC] void destroyCard(int index)
    {
        Destroy(myCardsGameObj[index]);
        myCardsGameObj.RemoveAt(index);
        myCards.RemoveAt(index);
        for(int i = 0;i<myCards.Count;i++)
        {
            myCards[i].myHandIndex = i;
        }
        CardAlignment();
    }

    public CardData PopItem()
    {
        if(cardDataBuffer.Count==0)
            SetupItemBuffer();

        CardData item=cardDataBuffer[0];
        cardDataBuffer.RemoveAt(0);
        return item;
    }

    void SetupItemBuffer(){
        cardDataBuffer = new List<CardData>(100);
        for(int i =0;i<cardDataSO.items.Length; i++)
        {
            CardData item = cardDataSO.items[i];
            cardDataBuffer.Add(item);
        }

        for(int i =0;i<cardDataBuffer.Count;i++)
        {
            int rand = Random.Range(i,cardDataBuffer.Count);
            CardData temp = cardDataBuffer[i];
            cardDataBuffer[i]=cardDataBuffer[rand];
            cardDataBuffer[rand]=temp;
        }
    }

    
    int handcount =0;
    void AddCard()
    {
        var cardObject = Instantiate(cardPrefab, new Vector2(-20,-20), Quaternion.identity);
        var card = cardObject.GetComponent<Card>();
        card.myHandIndex = handcount; handcount++;
        myCardsGameObj.Add(cardObject);
        card.Setup(PopItem(), PV.IsMine);
        myCards.Add(card);

        SetOriginOrder();
        CardAlignment();
    }

    void SetOriginOrder()
    {
        if(PV.IsMine)
        {
            int count = myCards.Count;
            for(int i = 0; i<count; i++)
            {
                var targetCard = myCards[i];
                targetCard.GetComponent<Card>().SetoriginOrder(i);
            }
        }
    }
    public void AlignAfter1sec()
    {
        StartCoroutine(CorAlignAfter1sec());
    }

    IEnumerator CorAlignAfter1sec()
    {
        yield return new WaitForSeconds(1f);
        PV.RPC("CardAlignment", RpcTarget.AllBuffered);
    }

    [PunRPC] void CardAlignment()
    {
        float gap = myCardsRight.x - myCardsLeft.x;
        if(myCards.Count==1)
        {
            if(PV.IsMine)    myCards[0].transform.position = new  Vector3(1,-4.2f,0);
            else myCards[0].transform.position = new Vector3(1, 4.2f, 0);
        }
        else
        {
            float interval = gap/(myCards.Count - 1);
            for(int i = 0;i<myCards.Count; i++)
            {
                myCards[i].transform.position = myCardsLeft + 
                new Vector3(interval*i,0,0);
            }
        }
    }


    [PunRPC]public void AddFiveCard()
    {
        StartCoroutine(AddFiveCards());
    }

    IEnumerator AddFiveCards()
    {
        AddCard();
        yield return new WaitForSeconds(0.2f);
        AddCard();
        yield return new WaitForSeconds(0.2f);
        AddCard();
        yield return new WaitForSeconds(0.2f);
        AddCard();
        yield return new WaitForSeconds(0.2f);
        AddCard();
    }


    [Header("Health Point")]
    public int MyHP = 3; int maxHP = 3;
    public GameObject hp1; public GameObject hp2; public GameObject hp3;

    public void GetDamaged()
    {
        MyHP--;
        renewalHPBar();
    }
    void renewalHPBar()
    {
        if(MyHP==2) hp3.SetActive(false);
        else if(MyHP==1) {hp3.SetActive(false); hp2.SetActive(false);}
        else
        {
            hp3.SetActive(false); hp2.SetActive(false);
            hp1.SetActive(false);
            if(PV.IsMine) GameManager.instance.LoseGame();
        }
    }

    
}