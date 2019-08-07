
using UnityEngine;
using UnityEngine.UI;

public class TitleBannerDemo:MonoBehaviour
{
    public TitleBanner Banner;
    public Color[] colors = new Color[]{Color.red, Color.black, Color.blue, Color.clear};
    void Start()
    {
//        if (sprites == null) return;
        Banner.Init(colors.Length, 3 , updateBanner , updateDot);
        Banner.Focus(0);
    }

    private void updateDot(GameObject arg1, int arg2, bool arg3)
    {
        
    }

    private void updateBanner(GameObject arg1, int arg2, bool arg3)
    {
        Color c = colors[arg2];
        c.a = arg3 ? 1 : 0.8f;
        arg1.GetComponent<Image>().color = c;
        arg1.transform.Find("Text").GetComponent<Text>().text = arg2.ToString();
    }
}
