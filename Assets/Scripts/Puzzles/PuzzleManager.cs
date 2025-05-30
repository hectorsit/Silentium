using NavKeypad;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.HighDefinition;

public class PuzzleManager : MonoBehaviour
{
    public static PuzzleManager instance { get; set; }

    [Header("Clock Puzzle")]
    [SerializeField]
    private GameObject KeyClock;
    [SerializeField]
    private Camera cam_Clock;
    [SerializeField]
    private Camera cam_Player;
    private InputSystem_Actions inputActionPlayer;
    public bool clockPuzzleCompleted { get; set; }

    [Header("Hieroglyphic Puzzle")]
    [SerializeField]
    private Camera cam_Hierogliphic;
    [SerializeField]
    private Camera cam_AIHierogliphic;
    [SerializeField]
    private AnimationClip doorsHieroglyphic;
    [SerializeField]
    private Animator hieroglyphicAnimator;
    [SerializeField]
    private Camera cam_HieroglyphicAnimation;    
    [SerializeField]
    private Animator hieroglyphicAnimator2;
    [SerializeField]
    private AnimationClip HieroglyphicAnimation2;
    [SerializeField]
    private Camera cam_HieroglyphicAnimation2;
    [SerializeField]
    public bool isHieroglyphicCompleted { get; set; } 
    [SerializeField]
    TextMeshProUGUI riddlerText;

    [Header("Book Puzzle")]
    [SerializeField]
    private BookPuzzle bookPuzzle;
    [SerializeField]
    private GameObject bookWall;
    public bool bookPuzzleCompleted { get; set; }
    [SerializeField]
    private GameObject book1;
    [SerializeField]
    private GameObject book2;
    [SerializeField]
    private GameObject book3;
    [SerializeField]
    private GameObject book4;
    [SerializeField]
    private GameObject book5;
    //Llibres de l'estanteria
    [SerializeField] private GameObject greenBook;
    [SerializeField] private GameObject redBook;
    [SerializeField] private GameObject yellowBook;
    [SerializeField] private GameObject greyBook;
    [SerializeField] private GameObject pinkBook;
    //Porta del puzle
    [SerializeField] private Door unlockDoor;

    [Header("Poem Puzzle")]
    [SerializeField]
    private List<Picture> pictureList;
    [SerializeField]
    public List<Picture> picturesClicked;
    [SerializeField]
    private Door DoorPoem3;
    public bool poemPuzzleCompleted { get; set; }

    [Header("Morse Puzzle")]
    [SerializeField] 
    private Player player;
    [SerializeField] 
    private InteractuableMorse morsePanel;
    [SerializeField] 
    private Camera cam_morse;
    [SerializeField] 
    private MorseKeypad morseKeypad;
    [SerializeField] 
    private GameObject cam_doorsMorseAnimation;
    [SerializeField] 
    private AnimationClip doorsMorseAnimation;
    [SerializeField] 
    private Animator morseAnimator;
    public bool isMorseCompleted { get; set; }

    [Header("Weapon Puzzle")]
    [SerializeField]
    private BoxCollider allWeapon;
    public bool weaponPuzzleCompleted { get; set; }
    [SerializeField] private Camera cam_WeaponPuzzle;
    [SerializeField] private InteractuableWeapon weapon;

    [Header("Glitch")]
    [SerializeField]
    AnimationClip glitchEffect;
    [SerializeField]
    Animator glitchAnimator;
    [SerializeField] bool glitchStarted = false;

    [Header("Positions after puzzles")]
    [SerializeField]
    private Transform positionAfterHieroglyphic;
    [SerializeField]
    private Transform positionAfterBook;
    [SerializeField]
    private Transform positionAfterPoem;

    [Header("All puzzles")]
    private float animationTime;
    [SerializeField]
    private AnimationClip fadeOut;
    private bool fadeOutStarted = false;
    private bool teleported = false;
    private Transform positionToTeleport;

    [Header("Glitch")]
    [SerializeField]
    private Material material;
    [SerializeField]
    private float noiseAmount;
    [SerializeField]
    private float glitchStrength;
    [SerializeField]
    private float scanLinesStrength;
    [SerializeField]
    private float FlickeringStrength;
    [SerializeField] 
    private Events events;

    [Header("Audio")]
    private AudioSource puzzlesAudioSource;
    [SerializeField]
    AudioClip glitchAudio;
    [SerializeField]
    AudioClip poemPuzzleAudio;

    private void Awake()
    {
        puzzlesAudioSource = this.GetComponent<AudioSource>();
        inputActionPlayer = new InputSystem_Actions();
        if (instance == null)
            instance = this;
    }

    private void Start()
    {
        clockPuzzleCompleted = false;
        isHieroglyphicCompleted = false;
        bookPuzzleCompleted = false;
        poemPuzzleCompleted = false;
        isMorseCompleted = false;
        weaponPuzzleCompleted = false;
        animationTime = 0f;
    }

    public void InteractClockPuzzle()
    {
        cam_Player.gameObject.SetActive(false);
        cam_Clock.gameObject.SetActive(true);
        cam_Player.transform.parent.position = new Vector3(-32.8191757f, 6.21000004f, -32.4704895f);
        cam_Player.transform.parent.rotation = new Quaternion(0, -0.608760536f, 0, 0.793354094f);
        player._inputActions.Player.Disable();
        player.ResumeInteract(false);
        events.ToggleCustomPass(false);
        events.ToggleUI(false);
        cam_Clock.transform.parent.GetComponent<Clock>().inputActions.Clock.Enable();
    }
    public void ExitClockPuzzle()
    {
        cam_Player.gameObject.SetActive(true);
        cam_Clock.gameObject.SetActive(false);
        player._inputActions.Player.Enable();
        player.ToggleInputPlayer(true, true);
        player.ResumeInteract(true);
        events.ToggleCustomPass(true);
        events.ToggleUI(true);
        cam_Clock.transform.parent.GetComponent<Clock>().inputActions.Clock.Disable();
    }
    public void ClockSolved()
    {
        ExitClockPuzzle();
        cam_Clock.transform.parent.GetComponent<InteractuableClock>().isInteractuable = false;
        player.ResumeInteract(true);
        this.KeyClock.SetActive(true);
        clockPuzzleCompleted = true;
    }

    private void ClockPuzzleLoad()
    {
        if (clockPuzzleCompleted)
        {
            cam_Clock.transform.parent.GetComponent<Clock>().inputActions.Clock.Disable();
            this.KeyClock.SetActive(false);
        }
    }

    public void InteractHieroglyphicPuzzle()
    {
        cam_Player.gameObject.SetActive(false);
        cam_Hierogliphic.gameObject.SetActive(true);
        cam_AIHierogliphic.gameObject.SetActive(true);
        player._inputActions.Player.Disable();
        player.ResumeInteract(false);
        events.ToggleCustomPass(false);
        cam_Hierogliphic.transform.parent.GetComponent<LineRendererExample>()._inputAction.Hieroglyphic.Enable();
        Cursor.visible = true;
        events.ToggleUI(false);
    }
    public void IsFlashlightning() { 
        if(player.flashlight.activeSelf)
            riddlerText.gameObject.SetActive(false);
        else
            riddlerText.gameObject.SetActive(true);
    }
    public void HieroglyphicPuzzleExitAnimation()
    {
        cam_Hierogliphic.gameObject.SetActive(false);
        cam_AIHierogliphic.gameObject.SetActive(false);
        cam_Hierogliphic.transform.parent.GetComponent<InteractuablePaint>().isInteractuable = false;
        cam_HieroglyphicAnimation.gameObject.SetActive(true);
        hieroglyphicAnimator.Play("HieroDoor");
        animationTime = 0f;
        isHieroglyphicCompleted = true;
    }

    public void HieroglyphicPuzzleExitAnimation2()
    {
        cam_HieroglyphicAnimation.gameObject.SetActive(false);
        cam_HieroglyphicAnimation2.gameObject.SetActive(true);
        hieroglyphicAnimator2.Play(HieroglyphicAnimation2.name);
    }
    public void HieroglyphicPuzzleExit(bool isCompleted)
    {
        if (isCompleted)
        {
            cam_HieroglyphicAnimation2.gameObject.SetActive(false);
            hieroglyphicAnimator2.enabled = false;
        }
        else
            cam_Hierogliphic.gameObject.SetActive(false);

        player._inputActions.Player.Enable();
        events.ToggleCustomPass(false);
        cam_Hierogliphic.transform.parent.GetComponent<LineRendererExample>()._inputAction.Hieroglyphic.Disable();
        player.ResumeInteract(true);
        cam_Player.gameObject.SetActive(true);
        Cursor.visible = false;
        events.ToggleUI(true);
    }

    private void HieroglyphicPuzzleLoad()
    {
        if(isHieroglyphicCompleted)
        {
            cam_Hierogliphic.gameObject.SetActive(false);
            cam_Hierogliphic.transform.parent.GetComponent<LineRendererExample>()._inputAction.Hieroglyphic.Disable();
            cam_Player.gameObject.SetActive(true);
            Cursor.visible = false;
            animationTime = 0f;
            unlockDoor.isLocked = false;
        }
    }

    public void ExitMorsePuzzleAnimation()
    {
        cam_morse.gameObject.SetActive(false);
        cam_doorsMorseAnimation.SetActive(true);
        morseAnimator.Play("FinalDoor");
        animationTime = 0f;
        isMorseCompleted = true;
        morsePanel.isInteractuable = false;
    }

    public void ExitMorsePuzzle(bool isCompleted)
    {
        if (isCompleted)
            cam_doorsMorseAnimation.SetActive(false);
        else
            cam_morse.gameObject.SetActive(false);
        
        player.ToggleInputPlayer(true, true);
        events.ToggleCustomPass(true);
        morseKeypad.inputActions.Morse.Disable();
        player.ResumeInteract(true);
        cam_Player.gameObject.SetActive(true);
        Cursor.visible = false;
        isMorseCompleted = true;
        events.ToggleUI(true);
    }

    public void InteractMorsePuzzle()
    {
        cam_Player.gameObject.SetActive(false);
        cam_morse.gameObject.SetActive(true);
        events.ToggleCustomPass(true);
        player._inputActions.Player.Disable();
        morseKeypad.inputActions.Morse.Enable();
        player.ResumeInteract(false);
        Cursor.visible = true;
        events.ToggleUI(false);
    }

    private void MorsePuzzleLoad()
    {
        if(isMorseCompleted)
        {
            morseKeypad.inputActions.Morse.Disable();
            Cursor.visible = false;
            cam_morse.gameObject.SetActive(false);
            cam_doorsMorseAnimation.SetActive(false);
        }
    }

    public void CheckBookPuzzle()
    {
        Debug.Log("CheckBookPuzzle");
        bookPuzzle.checkBookPosition();
    }

    public void BookPuzzleCompleted()
    {
        bookWall.SetActive(false);
        player.ToggleInputPlayer(false, false);
        positionToTeleport = positionAfterBook;
        events.ToggleCustomPass(false);
        glitchAnimator.Play("Glitch");
        glitchStarted = true;
        animationTime = 0f;
        bookPuzzleCompleted = true;
    }

    private void BookPuzzleLoad()
    {
        if(bookPuzzleCompleted)
        {
            animationTime = 0f;
            book1.SetActive(false);
            book2.SetActive(false);
            book3.SetActive(false);
            book4.SetActive(false);
            book5.SetActive(false);
            bookWall.SetActive(false);
            
            greenBook.SetActive(true);
            redBook.SetActive(true);
            greyBook.SetActive(true);
            yellowBook.SetActive(true);
            pinkBook.SetActive(true);
        }
    }

    public void TakePoemPart()
    {
        for (int i = 0; i < picturesClicked.Count; i++)
        {
            if (picturesClicked.ElementAt(i) == pictureList.ElementAt(i))
            {
                if (i == pictureList.Count - 1)
                {
                    for (int x = 0; x < picturesClicked.Count; x++)
                    {
                        puzzlesAudioSource.PlayOneShot(poemPuzzleAudio);
                        pictureList.ElementAt(x).GetComponent<InteractuablePicture>().isInteractuable = false;
                    }
                    DoorPoem3.isLocked = false;
                    events.ToggleUI(false);
                    player.ToggleInputPlayer(false, false);
                    positionToTeleport = positionAfterPoem;
                    puzzlesAudioSource.PlayOneShot(glitchAudio);
                    glitchAnimator.Play("Glitch");
                    glitchStarted = true;
                    animationTime = 0f;
                    poemPuzzleCompleted = true;
                }
            }
            else
            {
                picturesClicked.Clear();
            }
        }
    }

    private void PoemPuzzleLoad()
    {
        if(poemPuzzleCompleted)
        {
            DoorPoem3.isLocked = false;
            animationTime = 0f;
        }
    }

    public void InteractWeaponPuzzle()
    {
        cam_Player.gameObject.SetActive(false);
        cam_WeaponPuzzle.gameObject.SetActive(true);
        cam_WeaponPuzzle.transform.parent.GetComponent<WeaponPuzzle>().inputAction.WeaponPuzzle.Enable();
        player._inputActions.Player.Disable();
        events.ToggleCustomPass(false);
        allWeapon.enabled = false;
        Cursor.visible = true;
        player.ResumeInteract(false);
        events.ToggleUI(false);
    }

    public void ExitWeaponPuzzle()
    {
        cam_WeaponPuzzle.transform.parent.GetComponent<WeaponPuzzle>().inputAction.WeaponPuzzle.Disable();
        weapon.isInteractuable = false;
        player.ResumeInteract(true);
        events.ToggleCustomPass(true);
        player._inputActions.Player.Enable();
        cam_Player.gameObject.SetActive(true);
        cam_WeaponPuzzle.gameObject.SetActive(false);
        Cursor.visible = false;
        weaponPuzzleCompleted = true;
        events.ToggleUI(true);
    }

    private void WeaponPuzzleLoad()
    {
        if (weaponPuzzleCompleted)
        {
            cam_WeaponPuzzle.transform.parent.GetComponent<WeaponPuzzle>().inputAction.WeaponPuzzle.Disable();
            Cursor.visible = false;
            cam_WeaponPuzzle.gameObject.SetActive(false);
        }
    }

    void Update()
    {
        if (isMorseCompleted)
        {
            animationTime += Time.deltaTime;
            if (animationTime >= 2.4f)
            {
                ExitMorsePuzzle(true);
                animationTime = 0f;
                isMorseCompleted = false;
            }
        }
        // else if (isHieroglyphicCompleted)
        // {
        //     animationTime += Time.deltaTime;
        //     if (animationTime >= 2.4f)
        //     {
        //         HieroglyphicPuzzleExitAnimation2();
        //         animationTime = 0f;
        //         isHieroglyphicCompleted = false;
        //     }
        // }
        else if (glitchStarted)
        {
            animationTime += Time.deltaTime;
            if (animationTime >= 2f && !teleported)
            {
                player.GetComponent<CharacterController>().enabled = false;
                player.transform.position = positionToTeleport.position;
                player.GetComponent<CharacterController>().enabled = true;
                teleported = true;
                positionToTeleport = null;
            }
            if (animationTime >= glitchEffect.length && teleported)
            {
                animationTime = 0f;
                glitchStarted = false;
                player.ToggleInputPlayer(true, true);
                events.ToggleCustomPass(true);
                events.ToggleUI(true);
            }
        }
    }

    public void ChangePositionPlayerAfterHieroglyphic()
    {
        player.ToggleInputPlayer(false, false);
        glitchAnimator.Play("Glitch");
        puzzlesAudioSource.PlayOneShot(glitchAudio);
        glitchStarted = true;
        animationTime = 0f;
        positionToTeleport = positionAfterHieroglyphic;
    }

    public void LoadGame()
    {
        ClockPuzzleLoad();
        HieroglyphicPuzzleLoad();
        BookPuzzleLoad();
        PoemPuzzleLoad();
        MorsePuzzleLoad();
        WeaponPuzzleLoad();
    }
}
