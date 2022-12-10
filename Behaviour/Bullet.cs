using System.Collections;
using UnityEngine;

public class Bullet : MonoBehaviour
{
    [SerializeField]
    Collider body;
    [SerializeField]
    Material mat;
    [SerializeField]
    Light glow;

    private string team;
    private float speed = 75f;
    private CoreController cc;

    public void initialise(string team)
    {
        this.team = team;
        this.cc = GameObject.Find("GameMaster").GetComponent<CoreController>();
        glow.color = GameDefinitions.teams[team];
        StartCoroutine("collideAfterTime");
    }

    private IEnumerator collideAfterTime()
    {
        yield return new WaitForSeconds(0.001f); // Original 0.001f

        body.enabled = true;
    }

    void Update()
    {
        transform.Translate(Vector3.up * speed * Time.deltaTime);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (GlobalInfo.IsServer && GameDefinitions.teams.ContainsKey(collision.gameObject.tag) && !collision.gameObject.CompareTag(team)) {
            cc.SendKill(collision.gameObject);
        } // Not dealing with damage atm
        //Instantiate(explosion, collision.GetContact(0).point, Quaternion.identity, null);
        //explosion.Play();

        Destroy(gameObject);
    }
}
