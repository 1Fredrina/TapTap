using UnityEngine;
using UnityEngine.Events;

public abstract  class BasePanel : MonoBehaviour
{
    private CanvasGroup canvasGroup;
    public float alphaSpeed=10;

    public bool isShow = false;

    //当隐藏完毕后想执行的
    private UnityAction hideCallback=null;

    protected virtual void Awake()
    {        
        canvasGroup = this.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = this.gameObject.AddComponent<CanvasGroup>();
        }
    }
    protected virtual void Start()
    {
        Init();
    }

    public abstract void Init();

    public virtual void ShowMe()
    {
        canvasGroup.alpha = 0;
        isShow = true;
    }

    public virtual void HideMe(UnityAction callback)
    {
        canvasGroup.alpha = 1;
        isShow = false;
        hideCallback = callback;
    }


    // Update is called once per frame
    protected virtual void Update()
    {   
        if( isShow && canvasGroup.alpha != 1)
        {
            canvasGroup.alpha += Time.deltaTime * alphaSpeed;
            if(canvasGroup.alpha >= 1)
            {
                canvasGroup.alpha = 1;
            }
        }
        else if(!isShow && canvasGroup.alpha != 0)
        {
            canvasGroup.alpha -= Time.deltaTime * alphaSpeed;
            if(canvasGroup.alpha <= 0)
            {
                canvasGroup.alpha = 0;
                hideCallback?.Invoke();
            }
        }
    }


}
