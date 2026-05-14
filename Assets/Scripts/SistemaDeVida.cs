using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SistemaDeVida : MonoBehaviour
{
    public BarraDeVida barraDeVida;

    [Range(0, 100)] public float vidaMaxima = 100f;
    [Range(0, 100)] public float vidaAtual;

    protected Animator animator;
    protected bool estaMorto = false;

    protected virtual void Start()
    {
        animator = GetComponent<Animator>();

        vidaAtual = vidaMaxima;
        AtualizarVida();

        // Parâmetro "Vivo" começa ativo
        if (animator != null)
            animator.SetBool("Vivo", true);
    }

    public virtual void AplicarDano(float dano)
    {
        if (estaMorto) return;


        vidaAtual -= dano;

        if (animator != null)
            StartCoroutine(AnimacaoMachucado());
            AudioManager.Instance.Play("dano"); // toca só no início


        AtualizarVida();

        if (vidaAtual <= 0)
        {
            AudioManager.Instance.Play("dano"); // toca só no início
            Morrer();
        }
    }

    private IEnumerator AnimacaoMachucado()
    {
        if (animator != null)
        {
            animator.SetBool("Machucado", true);
            yield return new WaitForSeconds(0.5f);
            animator.SetBool("Machucado", false);
        }
    }

    protected void AtualizarVida()
    {
        if (barraDeVida != null)
            barraDeVida.AtualizarUI(vidaAtual / vidaMaxima);
    }

    protected virtual void Morrer()
    {
        if (estaMorto) return;
        estaMorto = true;
        AudioManager.Instance.Play("morte");


        if (animator != null)
            animator.SetBool("Vivo", false);

        StartCoroutine(ReiniciarCena());
    }

    private IEnumerator ReiniciarCena()
    {
        yield return new WaitForSeconds(15f);

        string cenaAtual = SceneManager.GetActiveScene().name;
        SceneManager.LoadScene(cenaAtual);
    }
}
