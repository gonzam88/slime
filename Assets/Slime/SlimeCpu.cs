using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using MidiJack;

public class SlimeCpu : MonoBehaviour
{
     private Vector2 cursorPos;

    // struct
    struct Particle
    {
        public Vector3 position;
        public Vector3 velocity;
        public float heading;
        public float speed;
    }

    /// <summary>
	/// Size in octet of the Particle struct.
    /// since float = 4 bytes...
    /// 4 floats = 16 bytes
	/// </summary>
    private const int SIZE_PARTICLE = sizeof(float)*8;

    /// <summary>
    /// Number of Particle created in the system.
    /// </summary>
    private int particleCount = 10000;

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
    public float speedMultiplier;
    
    public float sensorAngle;
    public float sensorDistance;
	
    [Header("UI")]
    public Text text;
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
            particleArray[i].position.x = 1.0f;
            particleArray[i].position.y = 1.0f;
            particleArray[i].position.z = 3.0f;

            particleArray[i].velocity.x =  Random.Range(-0.0001f, 0.0001f);
            particleArray[i].velocity.y =  Random.Range(-0.0001f, 0.0001f);
            particleArray[i].velocity.z = 0;

            particleArray[i].heading = Random.Range(0, 2* Mathf.PI); // RADIANS
            particleArray[i].speed = Random.Range(0.001f,0.05f);
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

       //Debug.Log(MidiMaster.GetKnob(0, 14));
        decaySpeed = map(MidiMaster.GetKnob(2), 0.0f, 1.0f, 1.1f, 0.8f);
        diffusionStrength = map(MidiMaster.GetKnob(1), 0.0f, 1.0f, -0.09f, 0.09f);
        speedMultiplier = MidiMaster.GetKnob(3);
        sensorAngle = map(MidiMaster.GetKnob(5), 0.0f, 1.0f, 0.0f, Mathf.PI);
        sensorDistance = map(MidiMaster.GetKnob(6), 0.0f, 1.0f, 0.01f, 2.0f);
       
        float[] mousePosition2D = { cursorPos.x, cursorPos.y };

        // Send datas to the compute shader
        computeShader.SetFloat("deltaTime", Time.deltaTime);
        computeShader.SetFloats("mousePosition", mousePosition2D);

        
        computeShader.SetFloat("decaySpeed", decaySpeed);
        computeShader.SetFloat("diffusionStrength", diffusionStrength);
        computeShader.SetFloat("speedMultiplier", speedMultiplier);
        computeShader.SetFloat("sensorAngle", sensorAngle);
        computeShader.SetFloat("sensorDistance", sensorDistance);
        
        // Update the Particles
        computeShader.Dispatch(mComputeShaderParticleKernelID, mWarpCount, 1, 1);
        computeShader.Dispatch(mComputeShaderTrailKernelID, 32, 32, 1);

        // UI
        text.text = "Decay Speed: " + decaySpeed + "\nDiffussion Strength: " + diffusionStrength;
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

    float map(float s, float a1, float a2, float b1, float b2)
    {
        return b1 + (s-a1)*(b2-b1)/(a2-a1);
    }
}
