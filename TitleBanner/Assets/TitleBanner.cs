using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public class TitleBanner : MonoBehaviour , IDragHandler , IBeginDragHandler , IEndDragHandler
{
    /// <summary>
    /// 创建点点
    /// </summary>
    public bool ShowDot;

    /// <summary>
    /// 点点prefab
    /// </summary>
    public GameObject DotPrefab;

    /// <summary>
    /// 点点父物体
    /// </summary>
    public Transform DotParent;

    /// <summary>
    /// banner子对象
    /// </summary>
    public GameObject ItemPrefab;

    /// <summary>
    /// banner父物体
    /// </summary>
    public Transform ItemParent;
    /// <summary>
    /// 自动轮播
    /// </summary>
    public bool AutoTurn;

    /// <summary>
    /// 自动轮播方向
    /// </summary>
    public bool TurnRight;

    /// <summary>
    /// 轮播间隔
    /// </summary>
    public float TurnInterval;

    /// <summary>
    /// 间隔宽度
    /// </summary>
    public float Width;

    /// <summary>
    /// 动画时长
    /// </summary>
    public float TweenTime;

    /// <summary>
    /// 刷新动画Scale
    /// </summary>
    public Vector3 ShakeScale;

    /// <summary>
    /// 刷新动画总时长
    /// </summary>
    public float ShakeTime;

    /// <summary>
    /// 左按钮
    /// </summary>
    public Button LeftButton;

    /// <summary>
    /// 右按钮
    /// </summary>
    public Button RightButton;

    /// <summary>
    /// 拖拽距离
    /// </summary>
    public float DragDistance;

    /// <summary>
    /// 是否接受拖拽事件
    /// </summary>
    public bool DragEnable = true;
    private int displayCount;
    private int centerViewIndex;

    private ViewNode tempNode;

    private GameObject[] dots;
    private Node[] nodes;
    private Node curNode;
    private ViewNode[] viewNodes;
    private ViewNode curViewNode;
    private Action<GameObject, int, bool> _onUpdateNode;
    private Action<GameObject, int, bool> _onUpdateDot;
    private List<Tween> tweenList = new List<Tween>();
    private float time;
    private float dragDis;
    private Vector3 dragBeginPos;
    private bool hasDragEventInvoke;
    private Coroutine cor;
    void Start()
    {
        if (LeftButton != null)
        {
            LeftButton.onClick.AddListener(() =>
            {
                MoveLeft();
            });
        }
        if (RightButton != null)
        {
            RightButton.onClick.AddListener(() =>
            {
                MoveRight();
            });
        }
    }

    void Update()
    {
        if (AutoTurn)
        {
            time += Time.deltaTime;
            if (time > TurnInterval)
            {
                if (TurnRight)
                {
                    MoveRight();
                }
                else
                {
                    MoveLeft();
                }

                time = 0;
            }
        }
    }

    public void Init(int allCount , int displayCount , Action<GameObject , int , bool> onUpdate , Action<GameObject , int , bool> onUpdateDot = null)
    {
        _onUpdateNode = onUpdate;
        _onUpdateDot = onUpdateDot;
        nodes = new Node[allCount];
        for (int i = 0; i < allCount; i++)
        {
            nodes[i] = new Node() {Value = i};
        }

        for (int i = 0; i < nodes.Length; i++)
        {
            int nextIndex = i + 1;
            if (nextIndex >= nodes.Length)
            {
                nextIndex -= nodes.Length;
            }
            int previousIndex = i - 1;
            if (previousIndex < 0)
            {
                previousIndex += nodes.Length;
            }
            nodes[i].Next = nodes[nextIndex];
            nodes[i].Previous = nodes[previousIndex];

        }


        viewNodes = new ViewNode[displayCount];
        centerViewIndex = displayCount / 2;
        for (int i = 0; i < displayCount; i++)
        {
            var item = Instantiate(ItemPrefab, ItemParent == null ? transform : ItemParent);
            item.SetActive(true);
            ViewNode node = new ViewNode(){gameObject = item , transform = item.transform , Index =  i};
            item.transform.localPosition = new Vector3((i - centerViewIndex) * Width , 0 , 0);
            viewNodes[i] = node;
        }
        var tempItem = Instantiate(ItemPrefab, ItemParent == null ? transform : ItemParent);
        tempItem.SetActive(false);
        tempNode = new ViewNode() { gameObject = tempItem, transform = tempItem.transform };

        for (int i = 0; i < displayCount; i++)
        {
            int nextIndex = i + 1;
            int previousIndex = i - 1;
            if (nextIndex < displayCount)
            {
                viewNodes[i].Next = viewNodes[nextIndex];
            }
            if (previousIndex >= 0)
            {
                viewNodes[i].Previous = viewNodes[previousIndex];
            }
        }

        
        curViewNode = viewNodes[centerViewIndex];

        if (ShowDot && DotPrefab != null)
        {
            dots = new GameObject[displayCount];
            for (int i = 0; i < displayCount; i++)
            {
                var dot = Instantiate(DotPrefab, DotParent ?? transform);
                dot.SetActive(true);
                dots[i] = dot;
            }
        }        
    }


    private void Focus(Node newNode)
    {
        for (int i = 0; i < tweenList.Count; i++)
        {
            tweenList[i].Kill();
        }
        tweenList.Clear();
        if (cor != null)
        {
            StopCoroutine(cor);   
        }
        if (curNode == null)
        {
            UpdateNode(newNode);
            //RefreshViewNode(newNode);
        }
        else
        {
            if (newNode == curNode)
            {
                return;
            }
            if (newNode == curNode.Previous || newNode == curNode.Next)
            {
                MoveNode(newNode);
            }
            else
            {
                RefreshViewNode(newNode);
            }
        }

        curNode = newNode;
        if (ShowDot && dots != null && _onUpdateDot != null)
        {
            for (int i = 0; i < dots.Length; i++)
            {
                _onUpdateDot(dots[i], i, i == curNode.Value);
            }
        }
        time = 0;
    }

    private void RefreshViewNode(Node node)
    {
        curViewNode.node = node;
        ViewNode leftViewNode = curViewNode.Previous;
        ViewNode rightViewNode = curViewNode.Next;
        Node leftNode = node.Previous;
        Node rightNode = node.Next;
        while (leftViewNode != null && rightViewNode != null)
        {
            leftViewNode.node = leftNode;
            rightViewNode.node = rightNode;
            leftNode = leftNode.Previous;
            rightNode = rightNode.Next;
            leftViewNode = leftViewNode.Previous;
            rightViewNode = rightViewNode.Next;
        }
        IterViewNode(curViewNode , view =>
        {
            var tween = view.transform.DOScale(ShakeScale, ShakeTime / 2);
            tween.OnKill(()=>
            {
                tweenList.Remove(tween);
                _onUpdateNode(view.gameObject, view.node.Value, view.node.Value == node.Value);
                var tween2 = view.transform.DOScale(Vector3.one, ShakeTime / 2);
                tweenList.Add(tween2);
                tween2.OnKill(() => { tweenList.Remove(tween2); });
            });
            tweenList.Add(tween);
        });
        UpdateViewNodeRenderDepth(curViewNode , curViewNode);
    }

    private void IterViewNode(ViewNode node , Action<ViewNode> call)
    {
        call(node);
        IterNext(node.Next , call);
        IterPrevious(node.Previous , call);
    }

    private void IterPrevious(ViewNode node , Action<ViewNode> call)
    {
        if (node != null)
        {
            call(node);
            IterPrevious(node.Previous, call);
        }
    }

    private void IterNext(ViewNode node, Action<ViewNode> call)
    {
        if (node != null)
        {
            call(node);
            IterNext(node.Next, call);
        }
    }

    private void MoveNode(Node newNode)
    {
        bool isMoveRight = newNode != curNode.Next ;
        int dis = isMoveRight ? 1 : -1;
        ViewNode tail = GetViewNodeTail(curViewNode, isMoveRight);
        ViewNode head = GetViewNodeHead(curViewNode, isMoveRight);
        
        var tailTween = tail.gameObject.transform.DOLocalMoveX((tail.Index  - dis - centerViewIndex) * Width , TweenTime);
        tail.transform.SetAsFirstSibling();
        tail.gameObject.name = "tail";
        tailTween.OnComplete(()=>
        {
            tail.gameObject.SetActive(false);
            tweenList.Remove(tailTween);
        }).OnKill(() =>
        {
            tail.gameObject.SetActive(false);
        });
        tweenList.Add(tailTween);
        
        if (isMoveRight)
        {
            tail.Previous.Next = null;
        }
        else
        {
            tail.Next.Previous = null;
        }

        tail.Previous = null;
        tail.Next = null;
        ViewNode normal = head;
        while (normal != null)
        {
            normal.Index += dis;
            tweenList.Add(normal.gameObject.transform.DOLocalMoveX((normal.Index - centerViewIndex)* Width, TweenTime));
            normal = normal.GetNext(isMoveRight);
            
        }

        ViewNode newViewNode = tempNode;
        newViewNode.gameObject.name = "new";
        newViewNode.gameObject.SetActive(true);
        if (isMoveRight)
        {
            head.Previous = newViewNode;
            newViewNode.Next = head;
            newViewNode.node = head.node.Previous;
        }
        else
        {
            head.Next = newViewNode;
            newViewNode.Previous = head;
            newViewNode.node = head.node.Next;
        }

        newViewNode.Index = head.Index - dis;
        newViewNode.transform.localPosition = new Vector3((head.Index - centerViewIndex)* Width , 0 , 0);

        newViewNode.transform.SetAsFirstSibling();
        tweenList.Add(newViewNode.transform.DOLocalMoveX((newViewNode.Index - centerViewIndex) * Width, TweenTime));

        tempNode = tail;
        
        curViewNode = curViewNode.GetNext(!isMoveRight);
        
        IterViewNode(curViewNode , view =>
        {
            _onUpdateNode(view.gameObject, view.node.Value, view == curViewNode);
        });

         cor = DelayRun(TweenTime - 0.2f, () => {
            UpdateViewNodeRenderDepth(curViewNode, curViewNode);
            tail.transform.SetAsFirstSibling();
            cor = null;
         });

    }


    Coroutine DelayRun(float time , Action call)
    {
        return StartCoroutine(delayRun(time, call));
    }

    IEnumerator delayRun(float time , Action call)
    {
        yield return new WaitForSeconds(time);
        call();
    }
    private void UpdateViewNodeRenderDepth(ViewNode left , ViewNode right)
    {
        while (left != null && right != null)
        {
            left.transform.SetAsFirstSibling();
            right.transform.SetAsFirstSibling();
            left = left.Previous;
            right = right.Next;
        }
    }

    private ViewNode GetViewNodeHead(ViewNode viewNode , bool  isRight)
    {
        if (isRight)
        {
            while (viewNode.Previous != null)
            {
                viewNode = viewNode.Previous;
            }
        }
        else
        {
            while (viewNode.Next != null)
            {
                viewNode = viewNode.Next;
            }
        }
        return viewNode;
    }

    private ViewNode GetViewNodeTail(ViewNode viewNode , bool isRight)
    {
        return GetViewNodeHead(viewNode, !isRight);
    }


    public void Focus(int index)
    {
        if (index < 0 || index >= nodes.Length)
        {
            Debug.LogError("index out of range " + index);
            return;
        }

        Node newNode = nodes[index];
        Focus(newNode);
    }

    private void UpdateNode(Node node)
    {
        _onUpdateNode(curViewNode.gameObject, node.Value, true);
        curViewNode.node = node;
        ViewNode leftViewNode = curViewNode.Previous;
        ViewNode rightViewNode = curViewNode.Next;
        Node leftNode = node.Previous;
        Node rightNode = node.Next;
        while (leftViewNode != null && rightViewNode != null)
        {
            _onUpdateNode(leftViewNode.gameObject, leftNode.Value, false);
            leftViewNode.node = leftNode;

            _onUpdateNode(rightViewNode.gameObject, rightNode.Value, false);
            rightViewNode.node = rightNode;
            leftNode = leftNode.Previous;
            rightNode = rightNode.Next;

            leftViewNode = leftViewNode.Previous;
            rightViewNode = rightViewNode.Next;
        }
    }


    

    [ContextMenu("MoveLeft")]
    public void MoveLeft()
    {
        if (curNode == null)
        {
            return;
        }
        Focus(curNode.Next);
    }


    [ContextMenu("MoveRight")]
    public void MoveRight()
    {
        if (curNode == null)
        {
            return;
        }
        Focus(curNode.Previous);
    }


    class Node
    {
        public int Value;
        public Node Previous;
        public Node Next;
    }

    class ViewNode
    {
        public int Index;
        public ViewNode Previous;
        public ViewNode Next;
        public GameObject gameObject;
        public Node node;
        public Transform transform;
        public ViewNode GetNext(bool isRight)
        {
            return isRight ? Next : Previous;
        }
        public ViewNode GetPrevious(bool isRight)
        {
            return isRight ? Previous : Next;
        }

    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!DragEnable)
        {
            return;
        }
        if (Math.Abs(eventData.position.x - dragBeginPos.x) > DragDistance && !hasDragEventInvoke)
        {
            hasDragEventInvoke = true;
            if (eventData.position.x > dragBeginPos.x)
            {
                MoveRight();
            }
            else
            {
                MoveLeft();
            }
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        dragBeginPos = eventData.position;

    }

    public void OnEndDrag(PointerEventData eventData)
    {
        hasDragEventInvoke = false;
    }
}
