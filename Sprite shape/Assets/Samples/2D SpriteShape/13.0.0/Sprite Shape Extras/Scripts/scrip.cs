using UnityEngine;

/// <summary>
/// Controlador 2D de personagem — movimento horizontal e pulo.
/// Requisitos:
///   - Adicione este script ao GameObject do personagem.
///   - O GameObject precisa ter um Rigidbody2D e um Collider2D.
///   - Crie uma Layer chamada "Ground" e marque o chão com ela.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController2D : MonoBehaviour
{
    [Header("Movimento")]
    [Tooltip("Velocidade horizontal de movimento")]
    public float velocidade = 5f;

    [Tooltip("Suavidade da parada (0 = brusco, 1 = desliza)")]
    [Range(0f, 1f)]
    public float suavidade = 0.05f;

    [Header("Pulo")]
    [Tooltip("Força do pulo")]
    public float forcaPulo = 12f;

    [Tooltip("Multiplicador de gravidade ao cair (queda mais pesada)")]
    public float multiplicadorQueda = 2.5f;

    [Tooltip("Multiplicador ao soltar o botão de pulo cedo (pulo curto)")]
    public float multiplicadorPuloCurto = 2f;

    [Tooltip("Tempo de coyote (segundos após sair da borda que ainda pode pular)")]
    public float tempoCoyote = 0.12f;

    [Tooltip("Janela de buffer do pulo (segundos antes de tocar o chão)")]
    public float bufferPulo = 0.1f;

    [Header("Chão")]
    [Tooltip("Transform do ponto de verificação do chão (crie um filho vazio embaixo do personagem)")]
    public Transform verificadorChao;

    [Tooltip("Raio da verificação de chão")]
    public float raioChao = 0.2f;

    [Tooltip("Layer que representa o chão")]
    public LayerMask layerChao;

    // --- estado interno ---
    private Rigidbody2D rb;
    private SpriteRenderer sr;
    private Animator anim;          // opcional — não quebra se não tiver Animator

    private bool noChao;
    private float tempoCoyoteRestante;
    private float tempoBufferPulo;
    private float velocidadeAtual;
    private Vector2 velocidadeSuave;

    // IDs dos parâmetros do Animator (opcional)
    private static readonly int AnimVelX = Animator.StringToHash("VelocidadeX");
    private static readonly int AnimNoChao = Animator.StringToHash("NoChao");
    private static readonly int AnimPulando = Animator.StringToHash("Pulando");

    // ---------------------------------------------------------------

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();   // pode ser null
        anim = GetComponent<Animator>();         // pode ser null

        // Evita que o personagem role
        rb.freezeRotation = true;
    }

    private void Update()
    {
        // --- verificar chão ---
        VerificarChao();

        // --- coyote time ---
        if (noChao)
            tempoCoyoteRestante = tempoCoyote;
        else
            tempoCoyoteRestante -= Time.deltaTime;

        // --- buffer de pulo ---
        if (Input.GetButtonDown("Jump"))
            tempoBufferPulo = bufferPulo;
        else
            tempoBufferPulo -= Time.deltaTime;

        // --- executar pulo ---
        if (tempoBufferPulo > 0f && tempoCoyoteRestante > 0f)
        {
            Pular();
            tempoBufferPulo = 0f;
            tempoCoyoteRestante = 0f;
        }

        // --- flip do sprite ---
        float entradaX = Input.GetAxisRaw("Horizontal");
        if (entradaX != 0 && sr != null)
            sr.flipX = entradaX < 0;

        // --- animações ---
        AtualizarAnimacoes(entradaX);
    }

    private void FixedUpdate()
    {
        // --- movimento horizontal ---
        float entradaX = Input.GetAxisRaw("Horizontal");
        float velAlvo = entradaX * velocidade;

        velocidadeAtual = Mathf.SmoothDamp(
            velocidadeAtual,
            velAlvo,
            ref velocidadeSuave.x,
            suavidade
        );

        rb.linearVelocity = new Vector2(velocidadeAtual, rb.linearVelocity.y);

        // --- gravidade melhorada ---
        AplicarGravidade();
    }

    // ---------------------------------------------------------------
    // Métodos privados
    // ---------------------------------------------------------------

    private void VerificarChao()
    {
        if (verificadorChao == null)
        {
            // fallback: usa a posição da base do collider
            Bounds b = GetComponent<Collider2D>().bounds;
            Vector2 origem = new Vector2(b.center.x, b.min.y);
            noChao = Physics2D.OverlapCircle(origem, raioChao, layerChao);
        }
        else
        {
            noChao = Physics2D.OverlapCircle(verificadorChao.position, raioChao, layerChao);
        }
    }

    private void Pular()
    {
        // Zera a velocidade vertical antes para pulos consistentes de plataformas em movimento
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);
        rb.AddForce(Vector2.up * forcaPulo, ForceMode2D.Impulse);
    }

    private void AplicarGravidade()
    {
        if (rb.linearVelocity.y < 0f)
        {
            // Caindo — gravidade mais pesada
            rb.linearVelocity += Vector2.up * Physics2D.gravity.y
                * (multiplicadorQueda - 1f) * Time.fixedDeltaTime;
        }
        else if (rb.linearVelocity.y > 0f && !Input.GetButton("Jump"))
        {
            // Pulo curto — soltou o botão antes do ápice
            rb.linearVelocity += Vector2.up * Physics2D.gravity.y
                * (multiplicadorPuloCurto - 1f) * Time.fixedDeltaTime;
        }
    }

    private void AtualizarAnimacoes(float entradaX)
    {
        if (anim == null) return;

        anim.SetFloat(AnimVelX, Mathf.Abs(entradaX));
        anim.SetBool(AnimNoChao, noChao);
        anim.SetBool(AnimPulando, rb.linearVelocity.y > 0.1f && !noChao);
    }

    // ---------------------------------------------------------------
    // Gizmos — visualiza o ponto de verificação de chão no Editor
    // ---------------------------------------------------------------
    private void OnDrawGizmosSelected()
    {
        Vector3 origem = verificadorChao != null
            ? verificadorChao.position
            : transform.position + Vector3.down * 0.5f;

        Gizmos.color = noChao ? Color.green : Color.red;
        Gizmos.DrawWireSphere(origem, raioChao);
    }
}