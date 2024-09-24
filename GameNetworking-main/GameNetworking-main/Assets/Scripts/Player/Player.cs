using System;
using Fusion;
using GNW2.Input;
using GNW2.Projectile;
using UnityEngine;

namespace GNW2.Player
{
    public class Player : NetworkBehaviour, ICombat
    {
        public bool gameWin = false;
        public GameObject playerHUD;
        [SerializeField] InformationText informationText;
        [SerializeField] private GameObject nearestTarget;

        [SerializeField] private GameObject winUI, loseUI;
        public int randomValue;
        public string playerName { get; set; }
        public Camera camera;

        public static Player Local { get; set; }
        private NetworkCharacterController _cc;
        [SerializeField] private Animator playerAnimation;
        private Vector3 _bulletSpawnLocation = Vector3.forward * 2;
        [SerializeField] private float speed = 8f;
        [SerializeField] private float projSpeed = 8f;
        [SerializeField] public float fireRate = 0.1f;
        [Networked] public TickTimer fireDelayTimer {  get; set; }
        public float delay;
        [SerializeField] private BulletScript bulletPrefab;
        //start
        public event Action<int> OnTakeDamage;
        //private bool canFire = true;
        //private event Action OnButtonPressed;

        void Update()
        {

            FindOtherPlayers();
            if (fireDelayTimer.ExpiredOrNotRunning(Runner))
            {
                informationText.readyToFire = true;
            }
            else
            {
                informationText.readyToFire = false;
            }
   
        }

        private void FindOtherPlayers()
        {
            GameObject[] targets;

            targets = GameObject.FindGameObjectsWithTag("Player");

            foreach (GameObject target in targets)
                {

                    if (target != this.gameObject)
                    {
                        nearestTarget = target;
                    }
                    
                }
        }
        public void OnCollisionEnter(Collision other)
        {

            if (other.gameObject.tag == "WinLine")
            {
                gameWin = true;
                CallWin();
            }


        }

        //end
        private void Awake()
        {

            informationText.readyToFire = true;
            _cc = GetComponent<NetworkCharacterController>();
            _cc.maxSpeed = speed;

            playerAnimation = GetComponentInChildren<Animator>();
        }

        public void RPC_Configure(string name, Color color)
        {
            playerName = name;
        }

        public override void Spawned()
        {
            randomValue = UnityEngine.Random.Range(0, 10000);

            if (HasInputAuthority)
            {
                playerHUD.gameObject.SetActive(true);
                Local = this;
                Camera.main.gameObject.SetActive(false);
            }
            else
            {
                Camera localCamera = GetComponentInChildren<Camera>();
                localCamera.enabled = false;
                playerHUD.gameObject.SetActive(false);
            }
        }


        public override void FixedUpdateNetwork()
        {

            if (!GetInput(out NetworkInputData data)) return;

            data.Direction.Normalize();
            _cc.Move(speed * data.Direction * Runner.DeltaTime);
            if (playerAnimation)
            {
                playerAnimation.SetBool("isWalking", data.Direction != Vector3.zero);
            }

            if (!HasStateAuthority || !fireDelayTimer.ExpiredOrNotRunning(Runner)) return;



            if (data.Direction.sqrMagnitude > 0)
            {
                _bulletSpawnLocation = data.Direction * 2f;
            }

            if (!data.buttons.IsSet(NetworkInputData.MOUSEBUTTON0)) return;

            fireDelayTimer = TickTimer.CreateFromSeconds(Runner, fireRate);
            Runner.Spawn(bulletPrefab, transform.position + _bulletSpawnLocation, Quaternion.LookRotation(_bulletSpawnLocation), Object.InputAuthority, OnBulletSpawned);

        }


        private void CallWin()
        {
            GameObject[] playerLists;
            playerLists = GameObject.FindGameObjectsWithTag("Player");

            foreach (GameObject target in playerLists)
            {

                if (target != this.gameObject)
                {
                    var e  = target.GetComponent<Player>();
                    e.loseUI.SetActive(true);
                }

            }
            if (gameWin)
            {
                winUI.SetActive(true);
            }
            else
            {
                loseUI.SetActive(true);
            }
        }
        /*
        [Rpc(targets: RpcTargets.Proxies)]
        void RPC_ShowLoseUI()
        {
            if()
        }
        */
        private void OnBulletSpawned(NetworkRunner runner, NetworkObject ob)
        {
            //float range = Mathf.Infinity;
            informationText.readyToFire = false;
            BulletScript bulletScript = ob.GetComponent<BulletScript>();

            bulletScript.bulletSpeed = projSpeed;
            if (nearestTarget != null)
            {
                bulletScript.target = nearestTarget;
            }
            bulletScript.Init();
        }
        public void TakeDamage(int Damage)
        {
            OnTakeDamage?.Invoke(Damage);
        }

    }
}
