using UnityEngine;

[RequireComponent(typeof(MeshRenderer))]
public class Parallax : MonoBehaviour
{
    private MeshRenderer mr;
    private Material mat;

    [Header("Behavior")]
    [Tooltip("If true, this layer moves exactly with pipes (use for ground).")]
    public bool matchPipesExactly = true;

    [Tooltip("For background layers: fraction of pipe speed (ignored if matchPipesExactly).")]
    [Range(0f, 1f)] public float parallaxRatio = 0.25f;

    [Tooltip("Scroll direction. Usually 1 for left movement, -1 to flip.")]
    public int direction = 1;

    [Header("Calibration")]
    [Tooltip("Visible width in world units (0 = auto).")]
    public float worldWidthOverride = 0f;

    private float uvPerWorldUnit = 0f;
    private float uvSpeed = 0f;
    private Vector2 uvOffset;
    private bool useBaseMap = false;

    private void Awake()
    {
        mr = GetComponent<MeshRenderer>();
        mat = mr.material;

        useBaseMap = mat.HasProperty("_BaseMap"); // URP vs Built-in
        CalibrateUvPerWorldUnit();

        // Subscribe to speed updates
        GameManager.OnPipeSpeedChanged += HandlePipeSpeedChange;

        // Initialize immediately from current pipe speed (global)
        float currentSpeed = GameManager.CurrentPipeSpeed;
        HandlePipeSpeedChange(currentSpeed);
    }

    private void OnDestroy()
    {
        GameManager.OnPipeSpeedChanged -= HandlePipeSpeedChange;
    }

    private void Update()
    {
        if (Mathf.Abs(uvSpeed) < 0.0001f) return;
        if (Time.timeScale <= 0f) return;

        uvOffset.x += uvSpeed * Time.deltaTime * direction;

        if (useBaseMap)
            mat.SetTextureOffset("_BaseMap", uvOffset);
        else
            mat.mainTextureOffset = uvOffset;
    }

    private void CalibrateUvPerWorldUnit()
    {
        float worldWidth = worldWidthOverride > 0f ? worldWidthOverride : mr.bounds.size.x;
        if (worldWidth <= 0f) worldWidth = 1f;

        float tilingX = useBaseMap ? mat.GetTextureScale("_BaseMap").x : mat.mainTextureScale.x;
        if (tilingX <= 0f) tilingX = 1f;

        uvPerWorldUnit = tilingX / worldWidth;
    }

    private void HandlePipeSpeedChange(float pipeSpeed)
    {
        // Convert world speed to UV scroll speed
        float worldToUV = uvPerWorldUnit > 0f ? uvPerWorldUnit : 0.001f;
        uvSpeed = pipeSpeed * worldToUV * (matchPipesExactly ? 1f : parallaxRatio);
    }
}
