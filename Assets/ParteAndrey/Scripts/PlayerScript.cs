using TMPro;
using UnityEngine;

public class PlayerScript : MonoBehaviour
{
    float speed = 10f;
    bool Pdpular;
    bool NoChao;
    int vida;
    Animator a;
    Rigidbody r;
    [SerializeField] public TextMeshProUGUI vidaTexto;

    void Start()
    {
        vida = 10;
        a = GetComponent<Animator>();
        r = GetComponent<Rigidbody>();
    }

    // Update is called once per frame
    void Update()
    {
        vidaTexto.text = vida.ToString();

        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");

        transform.Translate(new Vector3(x, 0, z) * speed * Time.deltaTime);

        if (Input.GetKeyDown(KeyCode.Space) && Pdpular)
        {
            pular();
        }

        if(x != 0 ||  z != 0)
        {
            a.SetBool("Correndo", true);
        }
        else
        {
            a.SetBool("Correndo", false);
        }

        if (NoChao)
        {
            a.SetBool("NoChao", true);
        }
        else
        {
            a.SetBool("NoChao", false);
        }
    }

    private void OnCollisionExit(Collision collision)
    {
        if(collision.gameObject.tag == "Chao")
        {
            NoChao = false;
            Pdpular = false;
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.tag == "Chao")
        {
            NoChao = true;
            Pdpular = true;
        }

        if (collision.gameObject.tag == "TomarDano")
        {
            vida--;   
        }
    }

    void pular()
    {
        r.AddForce(new Vector3 (0, 5, 0), ForceMode.Impulse);
    }


}
