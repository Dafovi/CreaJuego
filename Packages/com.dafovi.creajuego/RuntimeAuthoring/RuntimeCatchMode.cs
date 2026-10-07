using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

namespace CreaJuego.Web
{
    public sealed class RuntimeCatchSession:MonoBehaviour,IWorkshopSessionMetrics
    {
        int targetScore;
        public GameSessionState State{get;private set;}=GameSessionState.Playing;
        public int CurrentHealth{get;private set;}
        public int Score{get;private set;}
        public int EnemiesRemaining=>0;
        public string Objective=>$"Consigue {targetScore} puntos";

        public void Configure(int health,int target)
        {
            CurrentHealth=Mathf.Max(1,health);targetScore=Mathf.Max(1,target);Score=0;State=GameSessionState.Playing;
        }
        public bool Collect(int points)
        {
            if(State!=GameSessionState.Playing)return false;Score+=Mathf.Max(1,points);if(Score>=targetScore)State=GameSessionState.Won;return true;
        }
        public bool Hit(int damage)
        {
            if(State!=GameSessionState.Playing)return false;CurrentHealth=Mathf.Max(0,CurrentHealth-Mathf.Max(1,damage));if(CurrentHealth==0)State=GameSessionState.Lost;return true;
        }
        public string ConfigurationError()=>targetScore<=0?"El objetivo de puntos debe ser mayor que cero.":null;
    }

    [RequireComponent(typeof(Rigidbody2D),typeof(Collider2D),typeof(GameItem))]
    public sealed class RuntimeCatchPlayer:MonoBehaviour,IVisualMotionState
    {
        Rigidbody2D body;GameItem item;RuntimeCatchSession session;RuntimeLevelBounds bounds;float fixedY,immuneUntil;SpriteRenderer visual;
        public bool IsSupported=>true;
        public void Configure(RuntimeCatchSession game,RuntimeLevelBounds level)
        {
            session=game;bounds=level;body=GetComponent<Rigidbody2D>();item=GetComponent<GameItem>();visual=ItemVisual.Resolve(item);fixedY=Mathf.Clamp(transform.position.y,level.bottom+.5f,level.top-.5f);
            body.bodyType=RigidbodyType2D.Dynamic;body.gravityScale=0;body.freezeRotation=true;body.linearVelocity=Vector2.zero;body.simulated=true;
            foreach(var collider in GetComponents<Collider2D>()){collider.enabled=true;collider.isTrigger=false;}
        }
        void FixedUpdate()
        {
            if(body==null||session==null)return;float direction=0;if(session.State==GameSessionState.Playing&&Keyboard.current!=null){if(Keyboard.current.aKey.isPressed||Keyboard.current.leftArrowKey.isPressed)direction-=1;if(Keyboard.current.dKey.isPressed||Keyboard.current.rightArrowKey.isPressed)direction+=1;}
            float half=.35f;var collider=GetComponent<Collider2D>();if(collider!=null)half=Mathf.Max(.1f,collider.bounds.extents.x);float x=Mathf.Clamp(body.position.x,bounds.left+half,bounds.right-half);body.linearVelocity=new Vector2(direction*Mathf.Max(.2f,item.speed),0);body.position=new Vector2(x,fixedY);
        }
        public bool ReceiveDamage(int damage)
        {
            if(session==null||Time.time<immuneUntil||!session.Hit(damage))return false;immuneUntil=Time.time+.55f;if(isActiveAndEnabled)StartCoroutine(Flash());return true;
        }
        IEnumerator Flash(){if(visual==null)yield break;var color=visual.color;visual.color=new Color(1,.3f,.3f,1);yield return new WaitForSeconds(.14f);if(visual!=null)visual.color=color;}
    }

    [RequireComponent(typeof(Rigidbody2D),typeof(Collider2D),typeof(GameItem))]
    public sealed class RuntimeFallingObject:MonoBehaviour
    {
        Rigidbody2D body;GameItem item;RuntimeCatchSession session;RuntimeLevelBounds bounds;float spawnX,spawnY;
        public void Configure(RuntimeCatchSession game,RuntimeLevelBounds level)
        {
            session=game;bounds=level;body=GetComponent<Rigidbody2D>();item=GetComponent<GameItem>();spawnX=Mathf.Clamp(transform.position.x,level.left+.25f,level.right-.25f);spawnY=Mathf.Max(transform.position.y,level.top-.75f);
            body.bodyType=RigidbodyType2D.Kinematic;body.gravityScale=0;body.linearVelocity=Vector2.zero;body.simulated=true;
            foreach(var collider in GetComponents<Collider2D>()){collider.enabled=true;collider.isTrigger=true;}
        }
        void FixedUpdate()
        {
            if(body==null||session==null||session.State!=GameSessionState.Playing)return;body.MovePosition(body.position+Vector2.down*Mathf.Max(.5f,item.speed)*Time.fixedDeltaTime);if(body.position.y<bounds.bottom-.75f)Respawn();
        }
        void OnTriggerEnter2D(Collider2D other)
        {
            if(session==null||session.State!=GameSessionState.Playing)return;var player=other.GetComponentInParent<RuntimeCatchPlayer>();if(player==null)return;
            if(item.definition.kind==ItemKind.Prize)session.Collect(item.points);else if(item.definition.kind==ItemKind.Hazard)player.ReceiveDamage(item.damage);else return;Respawn();
        }
        public void Respawn(){if(body!=null)body.position=new Vector2(spawnX,spawnY);else transform.position=new Vector3(spawnX,spawnY,transform.position.z);}
    }
}
