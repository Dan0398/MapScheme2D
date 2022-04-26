using System.Threading.Tasks;
using System.IO;
using UnityEngine;

public class MapScreenshot
{
    static Vector2 CurrentResolution => new Vector2(3840, 2160);

    public async void RenderAsNewImage()
    {
        string Folder = await GetRenderImageFolder();
        if (string.IsNullOrEmpty(Folder)) return;
        Texture2D Screenshot = CreateCameraAndRender();
        if (Screenshot == null) return;
        WriteImageToDisk(Screenshot, Folder);
    }

    async Task<string> GetRenderImageFolder()
    {
        string ReadyPath = string.Empty;
        UnityEngine.EventSystems.EventSystem eventSystem = UnityEngine.EventSystems.EventSystem.current;
        eventSystem.gameObject.SetActive(false);
        SimpleFileBrowser.FileBrowser.ShowSaveDialog((string[] path) =>  ReadyPath = path[0], () => { }, SimpleFileBrowser.FileBrowser.PickMode.Files, false);
        while (SimpleFileBrowser.FileBrowser.IsOpen) await Task.Yield();
        eventSystem.gameObject.SetActive(true);
        return ReadyPath;
    }

    Texture2D CreateCameraAndRender()
    {
        Vector2 Resolution = CurrentResolution;
        GameObject CamObject = new GameObject();
        CamObject.transform.position = new Vector3(0, 0, -10);
        Camera CamComponent = CamObject.AddComponent<Camera>();
        CamComponent.orthographic = true;
        CamComponent.orthographicSize = Resolution.y * 0.5f;
        RenderTexture CamTexture = new RenderTexture(Mathf.RoundToInt(Resolution.x), Mathf.RoundToInt(Resolution.y), 1);
        RenderTexture.active = CamTexture;
        CamComponent.targetTexture = CamTexture;
        CamComponent.Render();
        Texture2D ReadyScreenshot = new Texture2D(CamTexture.width, CamTexture.height);
        ReadyScreenshot.ReadPixels(new Rect(0,0, ReadyScreenshot.width, ReadyScreenshot.height), 0, 0);
        ReadyScreenshot.Apply();
        RenderTexture.active = null;
        GameObject.Destroy(CamTexture);
        GameObject.Destroy(CamObject);
        return ReadyScreenshot;
    }

    async void WriteImageToDisk(Texture2D Target, string Path)
    {
        byte[] ScreenshotRaw = Target.EncodeToJPG();
        using (var Writer = new FileStream(Path + ".jpg", FileMode.Create, FileAccess.Write, FileShare.Write))
        {
            await Writer.WriteAsync(ScreenshotRaw, 0, ScreenshotRaw.Length);
            Writer.Close();
            Writer.Dispose();
        }
    }
}