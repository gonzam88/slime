using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SlimeCpu : MonoBehaviour
{
     private Vector2 cursorPos;

    // struct
    struct Particle
    {
        public Vector3 position;
        public Vector3 velocity;
        public float life;
    }

    /// <summary>
	/// Size in octet of the Particle struct.
    /// since float = 4 bytes...
    /// 4 floats = 16 bytes
	/// </summary>
	//private const int SIZE_PARTICLE = 24;
    private const int SIZE_PARTICLE = 28; // since property "life" is added...

    /// <summary>
    /// Number of Particle created in the system.
    /// </summary>
    private int particleCount = 100;

    /// <summary>
    /// Material used to draw the Particle on screen.
    /// </summary>
    public Material material;

    /// <summary>
    /// Compute shader used to update the Particles.
    /// </summary>
    public ComputeShader computeShader;
     /// <summary>


    /// <summary>
    /// Id of the kernel used.
    /// </summary>
    private int mComputeShaderParticleKernelID;
    /// <summary>
    /// Id of the kernel used trail.
    /// </summary>
    private int mComputeShaderTrailKernelID;

    /// <summary>
    /// Buffer holding the Particles.
    /// </summary>
    ComputeBuffer particleBuffer;

    /// <summary>
    /// Buffer holding the Trail.
    /// </summary>
    ComputeBuffer trailBuffer;

    

    /// <summary>
    /// Number of particle per warp.
    /// </summary>
    private const int WARP_SIZE = 256; // TODO?

    /// <summary>
    /// Number of warp needed.
    /// </summary>
    private int mWarpCount; // TODO?

    //public ComputeShader shader;

    // TRAIL Render
    public GameObject mainCanvas;
    static public RenderTexture particleTexture;
    public Image outputImageParticle;
    
    [Header("Settings")]
    public float decaySpeed;
    public float diffusionStrength;
	// Use this for initialization
	void Start () {
        InitTexture();
        InitCanvas();
        InitComputeShader();

    }

    public void InitTexture()
    {
        particleTexture = new RenderTexture(1024, 1024, 32);
        particleTexture.enableRandomWrite = true;
        particleTexture.Create();
    }

    void InitCanvas(){
        Canvas canvas = mainCanvas.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceCamera;
        canvas.worldCamera = Camera.main;
        CanvasScaler cs = mainCanvas.GetComponent<CanvasScaler>();
        cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        cs.referenceResolution = new Vector2(1920,1080);
        
        outputImageParticle.color = new Color(1,1,1,1);
        outputImageParticle.material.mainTexture = particleTexture;
        outputImageParticle.type = UnityEngine.UI.Image.Type.Simple;
        
    }
    
    void InitComputeShader()
    {
        mWarpCount = Mathf.CeilToInt((float)particleCount / WARP_SIZE);

        // initialize the particles
        Particle[] particleArray = new Particle[particleCount];

        for (int i = 0; i < particleCount; i++)
        {
            float x = 1.0f;//Random.value * 0.5f - 1.0f;
            float y = 1.0f;//Random.value * 0.5f - 1.0f;
            float z = 0.0f;//Random.value * 0.5f - 1.0f;
            Vector3 xyz = new Vector3(x, y, z);
            // xyz.Normalize();
            // xyz *= Random.value;
            // xyz *= 0.5f;


            particleArray[i].position.x = xyz.x;
            particleArray[i].position.y = xyz.y;
            particleArray[i].position.z = xyz.z + 3;

            particleArray[i].velocity.x =  Random.Range(-0.0001f, 0.0001f);
            particleArray[i].velocity.y =  Random.Range(-0.0001f, 0.0001f);
            particleArray[i].velocity.z = 0;

            // Initial life value
            particleArray[i].life = Random.value * 5.0f + 1.0f;
        }

        float[] trailArray = new float[1024*1024];
        for(int i = 0; i < 1024*1024; i++){
            trailArray[i] = 0;// Random.value;
        }

        // create compute buffer
        particleBuffer = new ComputeBuffer(particleCount, SIZE_PARTICLE);
        trailBuffer = new ComputeBuffer(1024*1024,  sizeof(float));

        particleBuffer.SetData(particleArray);
        trailBuffer.SetData(trailArray);

        // find the id of the kernel
        mComputeShaderParticleKernelID = computeShader.FindKernel("CSParticle");
        mComputeShaderTrailKernelID = computeShader.FindKernel("TrailDecay");


        // PARTICLE KERNEL
        // bind the compute buffer to the shader and the compute shader
        computeShader.SetBuffer(mComputeShaderParticleKernelID, "particleBuffer", particleBuffer);
        //computeShader.SetBuffer(mComputeShaderParticleKernelID, "TrailDecay", trailBuffer);
        //computeShader.SetBuffer(mComputeShaderParticleKernelID, "particleBuffer", trailBuffer);
        computeShader.SetBuffer(mComputeShaderParticleKernelID, "trailBuffer", trailBuffer);
        material.SetBuffer("particleBuffer", particleBuffer);


        // TRAIL KERNEL
        //computeShader.SetBuffer(mComputeShaderTrailKernelID, "TrailDecay", trailBuffer);  
        computeShader.SetBuffer(mComputeShaderTrailKernelID, "trailBuffer", trailBuffer);
        computeShader.SetTexture(mComputeShaderTrailKernelID, "Result", particleTexture);      
    }

    void OnRenderObject()
    {
        material.SetPass(0);
        Graphics.DrawProceduralNow(MeshTopology.Points, 1, particleCount);
    }

    void OnDestroy()
    {
        if (particleBuffer != null)
            particleBuffer.Release();
        if (trailBuffer != null)
            trailBuffer.Release();
    }

    // Update is called once per frame
    void Update () {

        float[] mousePosition2D = { cursorPos.x, cursorPos.y };

        // Send datas to the compute shader
        computeShader.SetFloat("deltaTime", Time.deltaTime);
        computeShader.SetFloats("mousePosition", mousePosition2D);

        
        computeShader.SetFloat("decaySpeed", decaySpeed);
        computeShader.SetFloat("diffusionStrength", diffusionStrength);
        


        // Update the Particles
        computeShader.Dispatch(mComputeShaderParticleKernelID, mWarpCount, 1, 1);
        computeShader.Dispatch(mComputeShaderTrailKernelID, 32, 32, 1);
    }

    void OnGUI()
    {
        Vector3 p = new Vector3();
        Camera c = Camera.main;
        Event e = Event.current;
        Vector2 mousePos = new Vector2();

        // Get the mouse position from Event.
        // Note that the y position from Event is inverted.
        mousePos.x = e.mousePosition.x;
        mousePos.y = c.pixelHeight - e.mousePosition.y;

        p = c.ScreenToWorldPoint(new Vector3(mousePos.x, mousePos.y, c.nearClipPlane + 14));// z = 3.

        cursorPos.x = p.x;
        cursorPos.y = p.y;
    }
}
