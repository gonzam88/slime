using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public struct Particle
{
    public float x;
    public float y;
    public float speed;
    public float maxSpeed;
    public float heading;

}

public struct Trail
{
    public float val;
}




public class SlimeCpu : MonoBehaviour
{
    public GameObject mainCanvas;
    static public RenderTexture particleTexture;
    public Image outputImageParticle;
    //static public RenderTexture trailTexture;
    //public Image outputImageTrail;
   

    static public ComputeShader _shader;
    [Header("Particle Data")]
    public static ComputeBuffer particlesBuffer;
    public int kernelParticleMove;
    public int cantParticles;
    public Particle[] particleArray;
    public float maxSpeed;

    [Header("Trail Data")]
    public static ComputeBuffer trailBuffer;
    public int kernelTrailDecay;
    public Trail[] trailArray;
    public int trailSize = 1024;

    // Start is called before the first frame update
    void Start()
    {
        InitTexture();
        InitCanvas();
        InitData();
        InitBuffers();
        InitShader();

    }

    public void InitTexture()
    {
        particleTexture = new RenderTexture(1024, 1024, 32);
        particleTexture.enableRandomWrite = true;
        particleTexture.Create();

        //trailTexture = new RenderTexture(1024, 1024, 32);
        //trailTexture.enableRandomWrite = true;
        //trailTexture.Create();

    }
    public void InitCanvas()
    {
        Canvas canvas = mainCanvas.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceCamera;
        canvas.worldCamera = Camera.main;
        CanvasScaler cs = mainCanvas.GetComponent<CanvasScaler>();
        cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        cs.referenceResolution = new Vector2(1920,1080);
        
        outputImageParticle.color = new Color(1,1,1,1);
        outputImageParticle.material.mainTexture = particleTexture;
        outputImageParticle.type = UnityEngine.UI.Image.Type.Simple;
        
        //outputImageTrail.color = new Color(1,1,1,1);
        //outputImageTrail.material.mainTexture = trailTexture;
        //outputImageTrail.type = UnityEngine.UI.Image.Type.Simple;
    }
    public void InitData()
    {
        particleArray = new Particle[cantParticles];
        for(int i=0; i<cantParticles; i++){
            particleArray[i].x = Random.Range(0, trailSize);
            particleArray[i].y = Random.Range(0, trailSize);
            particleArray[i].speed = .5f;
            particleArray[i].maxSpeed = maxSpeed;
        }

        trailArray = new Trail[trailSize*trailSize];
        for(int i=0; i<trailSize*trailSize; i++){
            trailArray[i].val = Random.Range(0.0f, 1.0f);
        }
    }
    public void InitBuffers()
    {
        particlesBuffer = new ComputeBuffer(trailSize*trailSize, sizeof(float)*5 ); //  modificar si cambia la cantidad de variables
        //particlesBuffer.SetData(particleArray);
        trailBuffer = new ComputeBuffer(trailSize*trailSize,  sizeof(float)); // Modificar si cambia la cant de variables o tipo
        
    }
    
    public void InitShader()
    {
        _shader = Resources.Load<ComputeShader>("slimeShader");
        //_shader.SetInt("cantParticles", cantParticles);
        
        kernelTrailDecay = _shader.FindKernel("TrailDecay");
        _shader.SetBuffer(kernelTrailDecay, "trailBuffer", trailBuffer);

        kernelParticleMove = _shader.FindKernel("ParticleMove");
        _shader.SetBuffer(kernelParticleMove, "trailBuffer", trailBuffer);
        _shader.SetBuffer(kernelParticleMove, "particlesBuffer", particlesBuffer);
        
        _shader.SetTexture(kernelParticleMove, "Result", particleTexture);
    }

    private void Update() {
        
        _shader.Dispatch(kernelTrailDecay, 32, 32, 1);
        _shader.Dispatch(kernelParticleMove, 32, 32, 1);
    }



    private void OnDestroy() {
        trailBuffer.Release();
        particlesBuffer.Release();
    }
}
