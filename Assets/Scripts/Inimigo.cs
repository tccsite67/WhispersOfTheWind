using System.Collections;
using UnityEngine;

public class Inimigo : MonoBehaviour
{
    [Header("Pontos de Patrulha")]
    public Transform pontoA;
    public Transform pontoB;

    [Header("Configurações")]
    public float moveSpeed = 2f;
    public float chaseSpeed = 3.5f;
    public float visionRange = 7f;
    public float visionHeight = 2f;
    public float distanciaAtaque = 0.9f; // similar ao comportamento por colisão

    [Header("Ataque")]
    public int danoAtaque = 10;
    public float tempoEntreAtaques = 0.7f;
    public float tempoStunado = 1;

    private bool atacando = false;
    private bool vivo = true;
    private bool isStunned = false;
    private bool vendoPlayer = false;

    [SerializeField] private bool movingRight = true;

    private Animator anim;
    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private Collider2D col;
    private Transform player;
    private float targetX = 0;

    [Header("Recompensa de XP")]
    public int xpDrop = 15;


    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        col = GetComponent<Collider2D>();
    }

    void Start()
    {
        GameObject p = GameObject.FindWithTag("Player");
        if (p != null) player = p.transform;

        OlharPara(targetX);
    }

    void Update()
    {
        if (!vivo || isStunned || atacando || player == null)
            return;

        DetectarPlayer();

        // Movimento e perseguição controlados por velocidade (não teleporta)
        if (vendoPlayer)
            PerseguirPlayer();
        else
            Patrulhar();

        ControlarAtaque();
        OlharPara(targetX);
    }

    // ----------------------------------------------------------
    // DETECÇÃO DE PLAYER (DISTÂNCIA)
    // ----------------------------------------------------------
    void DetectarPlayer()
    {
        float distancia = Vector2.Distance(transform.position, player.position);

        vendoPlayer =
            distancia <= visionRange &&
            Mathf.Abs(player.position.y - transform.position.y) <= visionHeight;
    }

    // ----------------------------------------------------------
    // PATRULHA
    // ----------------------------------------------------------
    void Patrulhar()
    {
        targetX = movingRight ? pontoB.position.x : pontoA.position.x;

        float direction = Mathf.Sign(targetX - transform.position.x);

        // define velocidade sem forçar reposicionamento
        rb.velocity = new Vector2(direction * moveSpeed, rb.velocity.y);

        // quando se aproxima suficientemente do alvo, troca direção
        if (Mathf.Abs(transform.position.x - targetX) < 0.12f)
            movingRight = !movingRight;

        anim.SetFloat("Velocidade", Mathf.Abs(rb.velocity.x));
    }

    // ----------------------------------------------------------
    // PERSEGUIÇÃO (RESPEITA LIMITES) - sem teleporte
    // ----------------------------------------------------------
    void PerseguirPlayer()
    {
        float limitLeft = Mathf.Min(pontoA.position.x, pontoB.position.x);
        float limitRight = Mathf.Max(pontoA.position.x, pontoB.position.x);

        float direction = (player.position.x > transform.position.x) ? 1f : -1f;

        // se o próximo passo ultrapassaria o limite, pare no limite (sem redefinir posição)
        float nextX = transform.position.x + direction * chaseSpeed * Time.deltaTime;

        // se queremos ir para a esquerda e nextX < limitLeft -> parar
        if (direction < 0f && nextX < limitLeft)
        {
            rb.velocity = new Vector2(0f, rb.velocity.y);
            // ficar olhando para o player, sem teleport
            anim.SetFloat("Velocidade", 0);
            return;
        }

        // se queremos ir para a direita e nextX > limitRight -> parar
        if (direction > 0f && nextX > limitRight)
        {
            rb.velocity = new Vector2(0f, rb.velocity.y);
            anim.SetFloat("Velocidade", 0);
            return;
        }

        // se está dentro dos limites, persegui normalmente
        movingRight = direction > 0f;
        rb.velocity = new Vector2(direction * chaseSpeed, rb.velocity.y);

        targetX = player.position.x;
        anim.SetFloat("Velocidade", Mathf.Abs(rb.velocity.x));
    }

    // ----------------------------------------------------------
    // ATAQUE POR DISTÂNCIA (independente de colisões)
    // ----------------------------------------------------------
    void ControlarAtaque()
    {
        float distancia = Vector2.Distance(transform.position, player.position);

        if (distancia <= distanciaAtaque && vivo)
        {
            if (!atacando)
                StartCoroutine(AtaqueInfinito());
        }
        else
        {
            // significa que o player saiu da distância de ataque
            atacando = false;
        }
    }

    IEnumerator AtaqueInfinito()
    {
        atacando = true;

        float distancia = Vector2.Distance(transform.position, player.position);
        SistemaDeVida vida = player.GetComponent<SistemaDeVida>();

        while (distancia <= distanciaAtaque && atacando && vivo)
        {
            anim.SetTrigger("Ataque");

            if (vida != null)
                vida.AplicarDano(danoAtaque);

            // mantém o ataque repetido na frequência desejada
            yield return new WaitForSeconds(tempoEntreAtaques);

            distancia = Vector2.Distance(transform.position, player.position);
        }

        atacando = false;
    }

    // ----------------------------------------------------------
    // DANO (STUN APENAS, sem knockback)
    // ----------------------------------------------------------
    public void AnimacaoDeDano()
    {
        if (!vivo) return;

        isStunned = true;
        anim.SetTrigger("Machucado");

        StartCoroutine(ResetStun());
    }

    IEnumerator ResetStun()
    {
        yield return new WaitForSeconds(tempoStunado);
        isStunned = false;
        anim.ResetTrigger("Machucado");
    }

    public void EfeitoDePiscar()
    {
        StartCoroutine(Piscar());
    }

    IEnumerator Piscar()
    {
        Color original = spriteRenderer.color;
        Color transparente = new Color(original.r, original.g, original.b, 0.5f);

        for (int i = 0; i < 3; i++)
        {
            spriteRenderer.color = transparente;
            yield return new WaitForSeconds(0.1f);
            spriteRenderer.color = original;
            yield return new WaitForSeconds(0.1f);
        }
    }

    // ----------------------------------------------------------
    // MORTE
    // ----------------------------------------------------------
    public void AnimacaoDeMorte()
    {
        vivo = false;

        // ENTREGA DE XP AO JOGADOR ------------------
        SistemaXP xp = FindObjectOfType<SistemaXP>();
        if (xp != null)
            xp.AdicionarXP(xpDrop);
        // -------------------------------------------

        rb.velocity = Vector2.zero;
        rb.isKinematic = true;
        col.enabled = false;

        anim.SetBool("Vivo", false);

        Destroy(gameObject, 3f);
    }


    // ----------------------------------------------------------
    // SUPORTE VISUAL
    // ----------------------------------------------------------

    void OlharPara(float x)
    {
        spriteRenderer.flipX = x < transform.position.x;
    }
}
