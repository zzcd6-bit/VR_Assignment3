using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LightGroupSwitcher : MonoBehaviour
{
    public enum SwitchMode
    {
        TwoGroupsPingPong,
        ThreeGroupsCycle,
        RandomLightsInEachGroup
    }

    [Header("切换模式")]
    [Tooltip("两组来回切 / 三组循环 / 每个灯组内部随机亮灯")]
    public SwitchMode switchMode = SwitchMode.TwoGroupsPingPong;

    [Header("灯组设置")]
    [Tooltip("把灯组1、灯组2、灯组3拖进来。每个灯组下面应该有多个子灯")]
    public GameObject[] lightGroups;

    [Header("普通切换时间")]
    [Tooltip("两组来回切、三组循环的切换间隔，单位：秒")]
    public float switchInterval = 1f;

    [Header("不规则随机亮灯设置")]
    [Tooltip("不规则模式下，最短切换时间")]
    public float randomMinInterval = 0.3f;

    [Tooltip("不规则模式下，最长切换时间")]
    public float randomMaxInterval = 1.2f;

    [Tooltip("一开始每个灯组随机亮几个灯")]
    public int startLightsPerGroup = 1;

    [Tooltip("每个灯组最多同时亮几个灯")]
    public int maxLightsPerGroup = 4;

    [Tooltip("达到最大亮灯数量后，是否重新从少量灯开始")]
    public bool resetAfterMax = true;

    [Header("防止连续重复设置")]
    [Tooltip("勾选后，上一轮亮过的灯，在下一轮不会再次被选中")]
    public bool avoidLastRoundLights = true;

    [Tooltip("如果可选的新灯不够，是否允许从上一轮亮过的灯里补选。建议先不要勾选")]
    public bool allowRepeatWhenNotEnough = false;

    [Header("子灯查找方式")]
    [Tooltip("推荐勾选。只控制每个灯组下面的直接子物体，不会影响更深层级")]
    public bool useDirectChildrenOnly = true;

    [Header("启动设置")]
    [Tooltip("物体启用时是否自动开始切换")]
    public bool autoStart = true;

    [Tooltip("关闭这个模式时，是否关闭所有灯组和子灯")]
    public bool turnOffAllWhenDisabled = true;

    private Coroutine switchCoroutine;
    private int currentIndex;
    private int currentLightsPerGroup;

    // 记录每个灯组上一轮亮过的灯的编号
    private Dictionary<int, List<int>> lastActiveIndexesByGroup = new Dictionary<int, List<int>>();

    private void OnEnable()
    {
        if (autoStart)
        {
            StartSwitching();
        }
    }

    private void OnDisable()
    {
        StopSwitching();

        if (turnOffAllWhenDisabled)
        {
            TurnOffAllGroupsAndChildren();
        }
    }

    public void StartSwitching()
    {
        StopSwitching();

        if (lightGroups == null || lightGroups.Length == 0)
        {
            Debug.LogWarning("StageLightGroupSwitcher：没有设置灯组。", this);
            return;
        }

        currentIndex = 0;
        currentLightsPerGroup = Mathf.Max(1, startLightsPerGroup);
        lastActiveIndexesByGroup.Clear();

        switchCoroutine = StartCoroutine(SwitchLoop());
    }

    public void StopSwitching()
    {
        if (switchCoroutine != null)
        {
            StopCoroutine(switchCoroutine);
            switchCoroutine = null;
        }
    }

    private IEnumerator SwitchLoop()
    {
        while (true)
        {
            switch (switchMode)
            {
                case SwitchMode.TwoGroupsPingPong:
                    TwoGroupsPingPong();
                    yield return new WaitForSeconds(switchInterval);
                    break;

                case SwitchMode.ThreeGroupsCycle:
                    ThreeGroupsCycle();
                    yield return new WaitForSeconds(switchInterval);
                    break;

                case SwitchMode.RandomLightsInEachGroup:
                    RandomLightsInEachGroup();
                    float waitTime = Random.Range(randomMinInterval, randomMaxInterval);
                    yield return new WaitForSeconds(waitTime);
                    break;
            }
        }
    }

    private void TwoGroupsPingPong()
    {
        if (lightGroups.Length < 2)
        {
            Debug.LogWarning("两组来回切换模式至少需要 2 个灯组。", this);
            return;
        }

        TurnOffAllGroups();

        int index = currentIndex % 2;

        if (lightGroups[index] != null)
        {
            lightGroups[index].SetActive(true);
            TurnOnAllChildLights(lightGroups[index]);
        }

        currentIndex++;
    }

    private void ThreeGroupsCycle()
    {
        if (lightGroups.Length < 3)
        {
            Debug.LogWarning("三组循环切换模式至少需要 3 个灯组。", this);
            return;
        }

        TurnOffAllGroups();

        int index = currentIndex % 3;

        if (lightGroups[index] != null)
        {
            lightGroups[index].SetActive(true);
            TurnOnAllChildLights(lightGroups[index]);
        }

        currentIndex++;
    }

    private void RandomLightsInEachGroup()
    {
        int targetLightsToTurnOn = Mathf.Max(1, currentLightsPerGroup);

        for (int groupIndex = 0; groupIndex < lightGroups.Length; groupIndex++)
        {
            GameObject group = lightGroups[groupIndex];

            if (group == null)
                continue;

            group.SetActive(true);

            List<GameObject> childLights = GetChildLightObjects(group);

            // 先关闭这个灯组里面所有子灯
            foreach (GameObject child in childLights)
            {
                if (child != null)
                    child.SetActive(false);
            }

            if (childLights.Count == 0)
                continue;

            List<int> lastIndexes = new List<int>();

            if (lastActiveIndexesByGroup.ContainsKey(groupIndex))
            {
                lastIndexes = lastActiveIndexesByGroup[groupIndex];
            }

            // 本轮可选灯：默认排除上一轮亮过的灯
            List<int> availableIndexes = new List<int>();

            for (int i = 0; i < childLights.Count; i++)
            {
                if (childLights[i] == null)
                    continue;

                bool wasActiveLastRound = lastIndexes.Contains(i);

                if (avoidLastRoundLights && wasActiveLastRound)
                    continue;

                availableIndexes.Add(i);
            }

            // 如果不够，并且允许补选，才把上一轮的灯加回来
            if (availableIndexes.Count < targetLightsToTurnOn && allowRepeatWhenNotEnough)
            {
                for (int i = 0; i < childLights.Count; i++)
                {
                    if (childLights[i] == null)
                        continue;

                    if (!availableIndexes.Contains(i))
                        availableIndexes.Add(i);
                }
            }

            int realLightCount = Mathf.Min(targetLightsToTurnOn, availableIndexes.Count);

            List<int> thisRoundActiveIndexes = new List<int>();

            for (int i = 0; i < realLightCount; i++)
            {
                int randomListIndex = Random.Range(0, availableIndexes.Count);
                int childIndex = availableIndexes[randomListIndex];

                childLights[childIndex].SetActive(true);
                thisRoundActiveIndexes.Add(childIndex);

                availableIndexes.RemoveAt(randomListIndex);
            }

            // 记录本轮亮过的灯，下一轮要避开
            if (lastActiveIndexesByGroup.ContainsKey(groupIndex))
            {
                lastActiveIndexesByGroup[groupIndex] = thisRoundActiveIndexes;
            }
            else
            {
                lastActiveIndexesByGroup.Add(groupIndex, thisRoundActiveIndexes);
            }
        }

        currentLightsPerGroup++;

        if (currentLightsPerGroup > maxLightsPerGroup)
        {
            if (resetAfterMax)
                currentLightsPerGroup = Mathf.Max(1, startLightsPerGroup);
            else
                currentLightsPerGroup = maxLightsPerGroup;
        }
    }

    private List<GameObject> GetChildLightObjects(GameObject group)
    {
        List<GameObject> result = new List<GameObject>();

        if (group == null)
            return result;

        if (useDirectChildrenOnly)
        {
            for (int i = 0; i < group.transform.childCount; i++)
            {
                Transform child = group.transform.GetChild(i);

                if (child != null)
                    result.Add(child.gameObject);
            }
        }
        else
        {
            Transform[] children = group.GetComponentsInChildren<Transform>(true);

            foreach (Transform child in children)
            {
                if (child == null)
                    continue;

                if (child == group.transform)
                    continue;

                result.Add(child.gameObject);
            }
        }

        return result;
    }

    private void TurnOnAllChildLights(GameObject group)
    {
        List<GameObject> childLights = GetChildLightObjects(group);

        foreach (GameObject child in childLights)
        {
            if (child != null)
                child.SetActive(true);
        }
    }

    private void TurnOffAllGroups()
    {
        if (lightGroups == null)
            return;

        foreach (GameObject group in lightGroups)
        {
            if (group != null)
                group.SetActive(false);
        }
    }

    private void TurnOffAllGroupsAndChildren()
    {
        if (lightGroups == null)
            return;

        foreach (GameObject group in lightGroups)
        {
            if (group == null)
                continue;

            List<GameObject> childLights = GetChildLightObjects(group);

            foreach (GameObject child in childLights)
            {
                if (child != null)
                    child.SetActive(false);
            }

            group.SetActive(false);
        }
    }
}