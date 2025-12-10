using System.Collections;
using UnityEngine;

public class BossScript : MonoBehaviour
{
    float vida;
    Animator a;
    Coroutine Transicao;
    int fase;
    void Start()
    {
        Transicao = null;
        vida = 100f;
        fase = 1;
        a = GetComponent<Animator>();
    }

    // Update is called once per frame
    void Update()
    {
        a.SetFloat("Life", vida);

        if (Input.GetKeyDown(KeyCode.T))
        {
            vida -= 10;
            print(vida);

            /*Fase 1: Maior igual a 70
              Fase 2: Menos de 70 e Maior Igual 30
              Fase 3: Menos de 30*/

            switch (fase)
            {
                case 1:
                    if(vida < 70) { checagemDeVida(); }
                    break;

                case 2:
                    if (vida < 30) { checagemDeVida(); }
                    break;
            }   
        }
    }

    void checagemDeVida()
    {
        

        if (Transicao == null)
        {
            Transicao = StartCoroutine(transicionando());
            print("Transicionando");
        }
        else
        {

        }
    }

    IEnumerator transicionando()
    {
        a.SetBool("Tran", true);
        fase++;

        yield return new WaitForSecondsRealtime(2f);
        a.SetBool("Tran", false);
        Transicao = null;
        print("Fim de transição");
    }
}
