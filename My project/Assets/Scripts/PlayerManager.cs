using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using TMPro;
using UnityEngine.UI;
using DG.Tweening;

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
        audioSource = this.gameObject.GetComponent<AudioSource>();
    }

    public List<CardData> cardDataBuffer;
    public cardDataSO cardDataSO;
    public GameObject cardPrefab;
    public PhotonView PV;
    AudioSource audioSource;
    public List<Card> myCards;
    public List<GameObject> myCardsGameObj;
    public Vector3 myCardsLeft;
    public Vector3 myCardsRight;

    private void Start() {
        SetupItemBuffer();
        transform.position = new Vector3(-2,-3.8f);
        if(PV.IsMine)
        {
            transform.position = new Vector3(-2,-3.8f,80);
            myCardsLeft = new Vector3(-0.5f,-4.2f,0);
            myCardsRight = new Vector3(2.24f,-4.2f,0);
        }
        else
        {
            transform.position = new Vector3(-2,3.8f,80);
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
        if(!PV.IsMine){
            StartCoroutine(cardflip(index));
            StartCoroutine(delay(index));}
        else {
            Destroy(myCardsGameObj[index]);
            myCardsGameObj.RemoveAt(index);
            myCards.RemoveAt(index);
            for(int i = 0;i<myCards.Count;i++)
            {
                myCards[i].myHandIndex = i;
            }
            CardAlignment();
        }
    }

    IEnumerator cardflip(int num) {
        Sequence seq=DOTween.Sequence();
        GameObject card=enemyPlayerManager.myCardsGameObj[num];
        Card cardscript=card.GetComponent<Card>();
        seq.Join(card.transform.DOMove(card.transform.position-new Vector3(0,5,0),0.75f).SetEase(Ease.OutQuad));
        seq.Join(cardscript.nameTMP.transform.DORotate(new Vector3(0,180,0),0.1f));
        seq.Join(cardscript.effectTMP.transform.DORotate(new Vector3(0,180,0),0.1f));
        seq.Append(card.transform.DORotate(new Vector3(0,180,0),0.5f));
        yield return new WaitForSeconds(0.96f);
        card.GetComponent<SpriteRenderer>().sprite=cardscript.cardFront;
        cardscript.characterSprite.sprite = cardscript.cardData.sprite;
        cardscript.nameTMP.text = cardscript.cardData.name;
        cardscript.effectTMP.text = cardscript.cardData.cardEffectInfoText;
    }

    IEnumerator delay(int index) {
        yield return new WaitForSeconds(3f);
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
        if(PV.IsMine) {
            var cardObject = Instantiate(cardPrefab,this.transform.position+new Vector3(30,20,0), Quaternion.identity);
            var card = cardObject.GetComponent<Card>();
            card.myHandIndex = handcount; handcount++;
            myCardsGameObj.Add(cardObject);
            card.Setup(PopItem(), PV.IsMine);
            myCards.Add(card);}
        else {
            var cardObject = Instantiate(cardPrefab,this.transform.position+new Vector3(30,-20,0), Quaternion.identity);
            var card = cardObject.GetComponent<Card>();
            card.myHandIndex = handcount; handcount++;
            myCardsGameObj.Add(cardObject);
            card.Setup(PopItem(), PV.IsMine);
            myCards.Add(card);
        }
        audioSource.Play();
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
            if(PV.IsMine)    myCards[0].transform.DOMove(new  Vector3(1,-4.2f,0),0.75f).SetEase(Ease.OutQuad);
            else myCards[0].transform.DOMove(new  Vector3(1,4.2f,0),0.75f).SetEase(Ease.OutQuad);
        }
        else
        {
            float interval = gap/(myCards.Count - 1);
            for(int i = 0;i<myCards.Count; i++)
            {
                myCards[i].transform.DOMove(myCardsLeft + 
                new Vector3(interval*i,0,0),0.75f).SetEase(Ease.OutQuad);
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
    public int MyHP = 3;
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
