using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class TheatreStageDirectorManager : MonoBehaviour
{
    public enum StageMode
    {
        Opening,
        Calm,
        BuildUp,
        Climax,
        Finale
    }

    public enum GameState
    {
        Tutorial,
        Opening,
        Playing,
        Result
    }

    [Serializable]
    public class StageModeEffect
    {
        [Header("模式基础信息")]
        [Tooltip("这个效果属于哪个舞台模式")]
        public StageMode mode;

        [Tooltip("方便你自己在 Inspector 里看，不会自动改 UI")]
        public string modeNameCN;

        [TextArea(2, 4)]
        [Tooltip("方便你自己在 Inspector 里看，不会自动改 UI")]
        public string modeDescriptionCN;

        [Tooltip("方便你自己在 Inspector 里看，不会自动改 UI")]
        public Sprite previewImage;

        [Header("这个模式要打开的物体")]
        [Tooltip("把这个模式对应的灯光、后处理 Volume、循环粒子组拖进来")]
        public GameObject[] activeObjects;

        [Header("切换到该模式时播放")]
        [Tooltip("切换到这个模式时播放一次的音效，可以不填")]
        public AudioClip enterSound;

        [Tooltip("切换到这个模式时播放一次的粒子，可以不填")]
        public ParticleSystem[] playParticlesOnEnter;
    }

    [Serializable]
    public class DancerAnimationInfo
    {
        [Header("舞者动画设置")]
        [Tooltip("舞者身上的 Animator")]
        public Animator animator;

        [Tooltip("是否在游戏开始时暂停这个舞者动画")]
        public bool pauseAtStart = true;
    }

    [Serializable]
    public class StageCue
    {
        [Header("得分点设置")]
        [Tooltip("这个得分点的名字，例如：First Build Up")]
        public string cueName;

        [Tooltip("从音乐正式开始播放后算起，多少秒时需要切换模式")]
        public float targetTime;

        [Tooltip("这个时间点需要切换到哪个模式")]
        public StageMode requiredMode;

        [Tooltip("这个得分点满分")]
        public int score = 10;

        [Header("判定范围")]
        [Tooltip("完美判定范围。例如 2 表示目标时间前后 2 秒内切换都算满分")]
        public float perfectWindow = 2f;

        [Tooltip("普通判定范围。例如 4 表示目标时间前后 4 秒内切换算一半分")]
        public float goodWindow = 4f;

        [HideInInspector]
        public bool scored;
    }

    private class CachedParticleObject
    {
        public GameObject particleObject;
        public ParticleSystem[] particleSystems;
        public float activeTime;
        public Coroutine coroutine;
    }

    [Header("当前状态")]
    [Tooltip("当前游戏状态。开始时是 Tutorial")]
    public GameState currentState = GameState.Tutorial;

    [Tooltip("当前舞台模式")]
    public StageMode currentMode = StageMode.Calm;

    [Header("五个模式的灯光 / 后处理 / 粒子设置")]
    [Tooltip("这里放 Opening、Calm、BuildUp、Climax、Finale 五个模式的效果资料")]
    public List<StageModeEffect> modeEffects = new List<StageModeEffect>();

    [Header("舞蹈和音乐")]
    [Tooltip("这里放 7 个舞者的 Animator")]
    public List<DancerAnimationInfo> dancers = new List<DancerAnimationInfo>();

    [Tooltip("舞蹈音乐 AudioSource")]
    public AudioSource musicSource;

    [Tooltip("开头模式持续时间。结束后会进入平淡模式，然后正式开始音乐和舞蹈")]
    public float openingDuration = 3f;

    [Header("分数系统")]
    [Tooltip("这里设置每一段需要切换到哪个模式。时间从音乐正式开始播放后开始算")]
    public List<StageCue> stageCues = new List<StageCue>();

    [Tooltip("表演总时长。如果填 0，就自动使用音乐长度")]
    public float performanceDuration = 0f;

    [Tooltip("一星需要达到的百分比")]
    [Range(0, 100)]
    public int oneStarPercent = 40;

    [Tooltip("二星需要达到的百分比")]
    [Range(0, 100)]
    public int twoStarPercent = 65;

    [Tooltip("三星需要达到的百分比")]
    [Range(0, 100)]
    public int threeStarPercent = 85;

    [Tooltip("结算面板。演出结束后才显示")]
    public GameObject resultPanel;

    [Tooltip("结算面板上的文字")]
    public TMP_Text resultText;

    [Header("烟花设置")]
    [Tooltip("烟花粒子。按烟花按钮时会播放，不计分")]
    public ParticleSystem[] fireworksParticles;

    [Tooltip("烟花音效")]
    public AudioClip fireworksSound;

    [Header("鼓掌彩蛋设置")]
    [Tooltip("鼓掌音效。不计分")]
    public AudioClip applauseSound;

    [Tooltip("鼓掌时播放的粒子，可不填")]
    public ParticleSystem[] applauseParticles;

    [Header("一次性粒子播放设置")]
    [Tooltip("如果粒子是 Looping，或者检测不到播放结束，就用这个时间后自动关闭物体")]
    public float particleFallbackActiveTime = 2f;

    [Tooltip("每次播放粒子前，是否先关闭再打开，让粒子从头播放")]
    public bool restartParticleObjectWhenPlay = true;

    [Header("音效播放源")]
    [Tooltip("用来播放按钮音效、掌声、烟花等音效的 AudioSource")]
    public AudioSource sfxAudioSource;

    [Header("提示 UI")]
    [Tooltip("开始前的教程面板，可不填")]
    public GameObject tutorialPanel;

    [Tooltip("状态提示文字")]
    public TMP_Text statusText;

    [Header("音画同步设置")]
    [Tooltip("音乐开始后，延迟多少秒再播放舞蹈。如果动作比音乐快，就增加这个值")]
    public float dancerStartDelay = 0.15f;

    private int totalScore;
    private int modeScore;
    private float performanceStartTime;

    private Dictionary<GameObject, CachedParticleObject> cachedParticleObjects =
        new Dictionary<GameObject, CachedParticleObject>();

    private void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;

        PauseAllDancersAtStart();

        currentState = GameState.Tutorial;

        if (resultPanel != null)
            resultPanel.SetActive(false);

        SetAllOneShotParticleObjectsInactive();
        BuildParticleCache();

        ApplyMode(StageMode.Calm);

        SetStatus(
            "Welcome to the Stage Director Challenge.\n" +
            "Try Calm, Build Up, Climax, and Finale modes first.\n" +
            "When you are ready, press Start Performance."
        );
    }

    private void Update()
    {
        if (currentState != GameState.Playing)
            return;

        float currentTime = GetPerformanceTime();

        if (currentTime >= GetPerformanceDuration())
        {
            EndPerformance();
        }
    }

    private void PauseAllDancersAtStart()
    {
        foreach (DancerAnimationInfo dancer in dancers)
        {
            if (dancer == null || dancer.animator == null)
                continue;

            if (!dancer.pauseAtStart)
                continue;

            dancer.animator.Rebind();
            dancer.animator.Update(0f);
            dancer.animator.speed = 0f;
        }
    }

    // =========================
    // 这些方法是给 Button OnClick 手动拖的
    // =========================

    public void PressCalmModeButton()
    {
        PressModeButton(StageMode.Calm);
    }

    public void PressBuildUpModeButton()
    {
        PressModeButton(StageMode.BuildUp);
    }

    public void PressClimaxModeButton()
    {
        PressModeButton(StageMode.Climax);
    }

    public void PressFinaleModeButton()
    {
        PressModeButton(StageMode.Finale);
    }

    public void PressStartButton()
    {
        if (currentState == GameState.Playing || currentState == GameState.Opening)
            return;

        StartCoroutine(StartPerformanceRoutine());
    }

    public void PressApplauseButton()
    {
        if (sfxAudioSource != null && applauseSound != null)
            sfxAudioSource.PlayOneShot(applauseSound);

        PlayParticleObjectsByParticleSystems(applauseParticles);

        if (currentState == GameState.Playing)
            SetStatus("Applause effect triggered!");
        else
            SetStatus("Applause preview triggered!");
    }

    public void PressFireworksButton()
    {
        PlayParticleObjectsByParticleSystems(fireworksParticles);

        if (sfxAudioSource != null && fireworksSound != null)
            sfxAudioSource.PlayOneShot(fireworksSound);

        if (currentState == GameState.Playing)
            SetStatus("Fireworks effect triggered!");
        else
            SetStatus("Fireworks preview triggered!");
    }

    public void PressModeButton(StageMode mode)
    {
        ApplyMode(mode);

        if (currentState == GameState.Tutorial)
        {
            SetStatus("Previewing: " + GetModeName(mode));
        }
        else if (currentState == GameState.Playing)
        {
            CheckModeScore(mode);
        }
    }

    private IEnumerator StartPerformanceRoutine()
    {
        currentState = GameState.Opening;

        ResetScoreSystem();

        if (resultPanel != null)
            resultPanel.SetActive(false);

        if (tutorialPanel != null)
            tutorialPanel.SetActive(false);

        SetStatus("Opening...");
        ApplyMode(StageMode.Opening);

        yield return new WaitForSeconds(openingDuration);

        ApplyMode(StageMode.Calm);

        if (musicSource != null)
        {
            musicSource.Stop();
            musicSource.time = 0f;
            musicSource.Play();
        }

        performanceStartTime = Time.time;

        if (dancerStartDelay > 0f)
        {
            yield return new WaitForSeconds(dancerStartDelay);
        }

        PlayAllDancers();

        currentState = GameState.Playing;

        SetStatus("Performance started! Switch modes at the correct timing.");
    }

    private void PlayAllDancers()
    {
        foreach (DancerAnimationInfo dancer in dancers)
        {
            if (dancer == null || dancer.animator == null)
                continue;

            dancer.animator.speed = 0f;
            dancer.animator.Rebind();
            dancer.animator.Update(0f);
            dancer.animator.speed = 1f;
        }
    }

    private void ApplyMode(StageMode mode)
    {
        currentMode = mode;

        foreach (StageModeEffect effect in modeEffects)
        {
            if (effect == null || effect.activeObjects == null)
                continue;

            foreach (GameObject obj in effect.activeObjects)
            {
                if (obj != null)
                    obj.SetActive(false);
            }
        }

        StageModeEffect selectedEffect = GetModeEffect(mode);

        if (selectedEffect == null)
        {
            SetStatus("Mode effect not found: " + mode);
            return;
        }

        if (selectedEffect.activeObjects != null)
        {
            foreach (GameObject obj in selectedEffect.activeObjects)
            {
                if (obj != null)
                    obj.SetActive(true);
            }
        }

        if (sfxAudioSource != null && selectedEffect.enterSound != null)
            sfxAudioSource.PlayOneShot(selectedEffect.enterSound);

        PlayParticleObjectsByParticleSystems(selectedEffect.playParticlesOnEnter);
    }

    private void ResetScoreSystem()
    {
        totalScore = 0;
        modeScore = 0;

        foreach (StageCue cue in stageCues)
        {
            if (cue != null)
                cue.scored = false;
        }
    }

    private void CheckModeScore(StageMode pressedMode)
    {
        float currentTime = GetPerformanceTime();

        StageCue bestCue = null;
        float bestDifference = float.MaxValue;

        foreach (StageCue cue in stageCues)
        {
            if (cue == null)
                continue;

            if (cue.scored)
                continue;

            if (cue.requiredMode != pressedMode)
                continue;

            float difference = Mathf.Abs(currentTime - cue.targetTime);

            if (difference <= cue.goodWindow && difference < bestDifference)
            {
                bestCue = cue;
                bestDifference = difference;
            }
        }

        if (bestCue == null)
        {
            SetStatus("Switched to: " + GetModeName(pressedMode));
            return;
        }

        bestCue.scored = true;

        int gainedScore;

        if (bestDifference <= bestCue.perfectWindow)
        {
            gainedScore = bestCue.score;
            SetStatus("Perfect! " + bestCue.cueName + " +" + gainedScore);
        }
        else
        {
            gainedScore = Mathf.CeilToInt(bestCue.score * 0.5f);
            SetStatus("Good! " + bestCue.cueName + " +" + gainedScore);
        }

        modeScore += gainedScore;
        totalScore += gainedScore;
    }

    private float GetPerformanceTime()
    {
        if (musicSource != null && musicSource.clip != null)
            return musicSource.time;

        return Time.time - performanceStartTime;
    }

    private float GetPerformanceDuration()
    {
        if (performanceDuration > 0f)
            return performanceDuration;

        if (musicSource != null && musicSource.clip != null)
            return musicSource.clip.length;

        return 60f;
    }

    private int GetMaxScore()
    {
        int maxScore = 0;

        foreach (StageCue cue in stageCues)
        {
            if (cue != null)
                maxScore += cue.score;
        }

        return maxScore;
    }

    private void EndPerformance()
    {
        currentState = GameState.Result;

        if (musicSource != null)
            musicSource.Stop();

        PauseAllDancersAtStart();

        int maxScore = GetMaxScore();
        float percent = maxScore > 0 ? (float)totalScore / maxScore * 100f : 0f;

        int stars = 0;

        if (percent >= threeStarPercent)
            stars = 3;
        else if (percent >= twoStarPercent)
            stars = 2;
        else if (percent >= oneStarPercent)
            stars = 1;

        string starText = GetStarText(stars);

        if (resultPanel != null)
            resultPanel.SetActive(true);

        if (resultText != null)
        {
            resultText.text =
                "Performance Complete!\n\n" +
                "Mode Timing Score: " + modeScore + " / " + maxScore + "\n" +
                "Completion: " + percent.ToString("0") + "%\n\n" +
                "Result: " + starText;
        }

        SetStatus("Performance Complete! Result: " + starText);
    }

    private string GetStarText(int stars)
    {
        if (stars == 1)
            return "1 Star";

        return stars + " Stars";
    }

    private void BuildParticleCache()
    {
        cachedParticleObjects.Clear();

        CacheParticleObjectsByParticleSystems(fireworksParticles);
        CacheParticleObjectsByParticleSystems(applauseParticles);

        foreach (StageModeEffect effect in modeEffects)
        {
            if (effect == null)
                continue;

            CacheParticleObjectsByParticleSystems(effect.playParticlesOnEnter);
        }
    }

    private void CacheParticleObjectsByParticleSystems(ParticleSystem[] particleSystems)
    {
        if (particleSystems == null)
            return;

        foreach (ParticleSystem ps in particleSystems)
        {
            if (ps == null)
                continue;

            GameObject particleObject = ps.gameObject;

            if (cachedParticleObjects.ContainsKey(particleObject))
                continue;

            CachedParticleObject data = new CachedParticleObject();

            data.particleObject = particleObject;
            data.particleSystems = particleObject.GetComponentsInChildren<ParticleSystem>(true);
            data.activeTime = CalculateParticleActiveTime(data.particleSystems);
            data.coroutine = null;

            cachedParticleObjects.Add(particleObject, data);
        }
    }

    private float CalculateParticleActiveTime(ParticleSystem[] particleSystems)
    {
        float maxTime = particleFallbackActiveTime;

        if (particleSystems == null)
            return maxTime;

        foreach (ParticleSystem ps in particleSystems)
        {
            if (ps == null)
                continue;

            ParticleSystem.MainModule main = ps.main;

            if (main.loop)
            {
                if (particleFallbackActiveTime > maxTime)
                    maxTime = particleFallbackActiveTime;

                continue;
            }

            float duration = main.duration;
            float lifetime = main.startLifetime.constantMax;

            float totalTime = duration + lifetime;

            if (totalTime > maxTime)
                maxTime = totalTime;
        }

        return maxTime;
    }

    private void PlayParticleObjectsByParticleSystems(ParticleSystem[] particleSystems)
    {
        if (particleSystems == null)
            return;

        foreach (ParticleSystem ps in particleSystems)
        {
            if (ps == null)
                continue;

            PlayParticleObject(ps.gameObject);
        }
    }

    private void PlayParticleObject(GameObject particleObject)
    {
        if (particleObject == null)
            return;

        if (!cachedParticleObjects.ContainsKey(particleObject))
        {
            CachedParticleObject newData = new CachedParticleObject();

            newData.particleObject = particleObject;
            newData.particleSystems = particleObject.GetComponentsInChildren<ParticleSystem>(true);
            newData.activeTime = CalculateParticleActiveTime(newData.particleSystems);
            newData.coroutine = null;

            cachedParticleObjects.Add(particleObject, newData);
        }

        CachedParticleObject data = cachedParticleObjects[particleObject];

        if (data.coroutine != null)
        {
            StopCoroutine(data.coroutine);
            data.coroutine = null;
        }

        data.coroutine = StartCoroutine(PlayParticleObjectRoutine(data));
    }

    private IEnumerator PlayParticleObjectRoutine(CachedParticleObject data)
    {
        if (data == null || data.particleObject == null)
            yield break;

        if (restartParticleObjectWhenPlay)
        {
            data.particleObject.SetActive(false);
            yield return null;
        }

        data.particleObject.SetActive(true);

        if (data.particleSystems != null)
        {
            foreach (ParticleSystem ps in data.particleSystems)
            {
                if (ps == null)
                    continue;

                ps.Clear(true);
                ps.Play(true);
            }
        }

        yield return new WaitForSeconds(data.activeTime);

        if (data.particleObject != null)
            data.particleObject.SetActive(false);

        data.coroutine = null;
    }

    private void SetAllOneShotParticleObjectsInactive()
    {
        SetParticleObjectsInactiveByParticleSystems(fireworksParticles);
        SetParticleObjectsInactiveByParticleSystems(applauseParticles);

        foreach (StageModeEffect effect in modeEffects)
        {
            if (effect == null)
                continue;

            SetParticleObjectsInactiveByParticleSystems(effect.playParticlesOnEnter);
        }
    }

    private void SetParticleObjectsInactiveByParticleSystems(ParticleSystem[] particleSystems)
    {
        if (particleSystems == null)
            return;

        foreach (ParticleSystem ps in particleSystems)
        {
            if (ps == null)
                continue;

            ps.gameObject.SetActive(false);
        }
    }

    private StageModeEffect GetModeEffect(StageMode mode)
    {
        foreach (StageModeEffect effect in modeEffects)
        {
            if (effect != null && effect.mode == mode)
                return effect;
        }

        return null;
    }

    private string GetModeName(StageMode mode)
    {
        switch (mode)
        {
            case StageMode.Opening:
                return "Opening";
            case StageMode.Calm:
                return "Calm";
            case StageMode.BuildUp:
                return "Build Up";
            case StageMode.Climax:
                return "Climax";
            case StageMode.Finale:
                return "Finale";
            default:
                return mode.ToString();
        }
    }

    private void SetStatus(string message)
    {
        if (statusText != null)
            statusText.text = message;
    }
}