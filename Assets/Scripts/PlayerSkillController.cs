using UnityEngine;
using UnityEngine.VFX;
using System.Collections;

public class PlayerSkillController : MonoBehaviour
{
    [System.Serializable]
    public class Skill
    {
        public string name;
        public KeyCode key;
        public GameObject skillEffectPrefab;
        public float cooldown = 5f;
        public bool useMouseDirection = false;
        public float castDistance = 5f;
        public float activeDuration = 3f;

        [HideInInspector] public GameObject currentVFX;
        [HideInInspector] public bool isPreparing;
        [HideInInspector] public float cooldownTimer;
    }

    public Skill[] skills;
    public LayerMask groundLayer;
    private Camera cam;

    void Start()
    {
        cam = Camera.main;
    }

    void Update()
    {
        foreach (var skill in skills)
        {
            skill.cooldownTimer -= Time.deltaTime;

            if (Input.GetKeyDown(skill.key))
            {
                if (skill.isPreparing)
                {
                    skill.isPreparing = false;
                    Debug.Log($"Canceled {skill.name}");
                    return;
                }

                if (skill.cooldownTimer <= 0f)
                {
                    if (skill.useMouseDirection)
                    {
                        skill.isPreparing = true;
                        Debug.Log($"Preparing {skill.name}...");
                    }
                    else
                    {
                        TriggerSkill(skill, GetPlayerCenter());
                        skill.cooldownTimer = skill.cooldown;
                    }
                }
            }
        }

        foreach (var skill in skills)
        {
            if (skill.isPreparing && Input.GetMouseButtonDown(0))
            {
                Vector3? castPos = GetMouseCastPoint(skill.castDistance);
                if (castPos.HasValue)
                {
                    TriggerSkill(skill, castPos.Value);
                    skill.cooldownTimer = skill.cooldown;
                }
                else
                {
                    Debug.Log("ตำแหน่งคลิกอยู่นอกระยะ");
                }

                skill.isPreparing = false;
            }
        }
    }

    void TriggerSkill(Skill skill, Vector3 position)
    {
        if (skill.currentVFX == null)
        {
            // ❗ ปรับตำแหน่งให้อยู่ที่ผู้เล่นเสมอ ไม่ว่า mouse หรือไม่
            Vector3 spawnPosition = IsShieldSkill(skill) ? GetPlayerCenter() : position;
            skill.currentVFX = Instantiate(skill.skillEffectPrefab, spawnPosition, Quaternion.identity);

            if (IsShieldSkill(skill))
                StartCoroutine(UpdateShieldFollowPosition(skill));
        }
        else
        {
            skill.currentVFX.transform.position = position;
            skill.currentVFX.SetActive(false);
            skill.currentVFX.SetActive(true);
        }

        var vfx = skill.currentVFX.GetComponent<VisualEffect>();
        if (vfx != null)
        {
            if (IsShieldSkill(skill))
            {
                if (vfx.HasVector3("ShieldFollowPosition"))
                    vfx.SetVector3("ShieldFollowPosition", GetPlayerCenter());
            }
            else
            {
                if (vfx.HasVector3("TargetPosition"))
                    vfx.SetVector3("TargetPosition", position);
            }
        }

        Debug.Log($"Cast {skill.name} at {position}");
        StartCoroutine(DeactivateSkillAfter(skill, skill.activeDuration));
    }

    IEnumerator UpdateShieldFollowPosition(Skill skill)
    {
        float timer = 0f;
        var vfx = skill.currentVFX.GetComponent<VisualEffect>();
        if (vfx == null) yield break;

        yield return null; // ❗ รอให้ VFX พร้อมก่อน 1 เฟรม

        while (timer < skill.activeDuration)
        {
            if (vfx.HasVector3("ShieldFollowPosition"))
                vfx.SetVector3("ShieldFollowPosition", GetPlayerCenter());

            timer += Time.deltaTime;
            yield return null;
        }
    }

    IEnumerator DeactivateSkillAfter(Skill skill, float delay)
    {
        yield return new WaitForSeconds(delay);

        if (skill.currentVFX != null)
        {
            Destroy(skill.currentVFX);
            skill.currentVFX = null;
        }
    }

    Vector3? GetMouseCastPoint(float maxDistance)
    {
        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, 100f, groundLayer))
        {
            Vector3 point = hit.point;
            point.y = 0.01f;

            float distance = Vector3.Distance(GetPlayerCenter(), point);
            if (distance <= maxDistance)
                return point;
        }

        return null;
    }

    Vector3 GetPlayerCenter()
    {
        Vector3 pos = transform.position;
        pos.y = 1f; // ✅ ตามที่คุณกำหนดให้โล่ลอย
        return pos;
    }

    bool IsShieldSkill(Skill skill)
    {
        string n = skill.name.ToLower();
        return n.Contains("shield") || n.Contains("bubble");
    }
}
