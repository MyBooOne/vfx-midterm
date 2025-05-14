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
        public AnimationClip animationClip;

        [HideInInspector] public GameObject currentVFX;
        [HideInInspector] public bool isPreparing;
        [HideInInspector] public float cooldownTimer;
    }

    public Skill[] skills;
    public LayerMask groundLayer;
    public Animator animator;

    // ✅ Passive form
    public SkinnedMeshRenderer meshRenderer;
    public Material normalMaterial;
    public Material passiveMaterial;
    public float passiveDuration = 5f;
    private bool isInPassiveForm = false;

    private Camera cam;

    void Start()
    {
        cam = Camera.main;
    }

    void Update()
    {
        // สลับเป็นร่าง Passive เมื่อกด X
        if (Input.GetKeyDown(KeyCode.X) && !isInPassiveForm)
        {
            EnterPassiveForm();
        }

        // ใช้งานสกิลตามปกติ (ยกเว้นตอนอยู่ใน Passive)
        if (!isInPassiveForm)
        {
            foreach (var skill in skills)
            {
                skill.cooldownTimer -= Time.deltaTime;

                if (Input.GetKeyDown(skill.key))
                {
                    if (skill.isPreparing)
                    {
                        skill.isPreparing = false;
                        animator.SetBool(skill.name, false);
                        return;
                    }

                    if (skill.cooldownTimer <= 0f)
                    {
                        if (skill.useMouseDirection)
                        {
                            skill.isPreparing = true;
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
                    skill.isPreparing = false;
                }
            }
        }
    }

    void TriggerSkill(Skill skill, Vector3 position)
    {
        ResetAllAnimationBools();

        if (animator != null && animator.HasParameter(skill.name))
        {
            animator.SetBool(skill.name, true);
            StartCoroutine(ResetAnimatorBool(skill.name, skill.animationClip.length));
        }

        if (skill.currentVFX == null)
        {
            Vector3 spawnPosition = IsShieldSkill(skill) ? GetPlayerCenter() : position;
            skill.currentVFX = Instantiate(skill.skillEffectPrefab, spawnPosition, Quaternion.identity);

            if (IsShieldSkill(skill))
                StartCoroutine(UpdateShieldFollowPosition(skill));

            if (skill.key == KeyCode.Q)
                StartCoroutine(AnimateSkillScale(skill));
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

        StartCoroutine(DeactivateSkillAfter(skill, skill.activeDuration));
    }

    void ResetAllAnimationBools()
    {
        foreach (var skill in skills)
        {
            if (animator.HasParameter(skill.name))
            {
                animator.SetBool(skill.name, false);
            }
        }
    }

    IEnumerator ResetAnimatorBool(string paramName, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (animator != null && animator.HasParameter(paramName))
        {
            animator.SetBool(paramName, false);
        }
    }

    IEnumerator UpdateShieldFollowPosition(Skill skill)
    {
        float timer = 0f;
        var vfx = skill.currentVFX.GetComponent<VisualEffect>();
        if (vfx == null) yield break;

        yield return null;

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

    IEnumerator AnimateSkillScale(Skill skill)
    {
        float halfDuration = skill.activeDuration / 2f;
        float timer = 0f;

        Transform fxTransform = skill.currentVFX.transform;
        fxTransform.localScale = Vector3.zero;

        while (timer < halfDuration)
        {
            float t = timer / halfDuration;
            fxTransform.localScale = Vector3.Lerp(Vector3.zero, Vector3.one, t);
            timer += Time.deltaTime;
            yield return null;
        }

        timer = 0f;
        while (timer < halfDuration)
        {
            float t = timer / halfDuration;
            fxTransform.localScale = Vector3.Lerp(Vector3.one, Vector3.zero, t);
            timer += Time.deltaTime;
            yield return null;
        }

        fxTransform.localScale = Vector3.zero;
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
        pos.y = 1f;
        return pos;
    }

    bool IsShieldSkill(Skill skill)
    {
        string n = skill.name.ToLower();
        return n.Contains("shield") || n.Contains("bubble");
    }

    // -------------------------------
    // ✅ Passive Form Logic
    // -------------------------------
    void EnterPassiveForm()
    {
        isInPassiveForm = true;

        // เปลี่ยน Material เป็นร่าง passive
        if (meshRenderer != null && passiveMaterial != null)
            meshRenderer.material = passiveMaterial;

        // เล่นท่า PassiveForm (ต้องมี State และ Bool ตรงชื่อ)
        if (animator != null && animator.HasParameter("PassiveForm"))
        {
            animator.SetBool("PassiveForm", true);
        }

        StartCoroutine(ReturnFromPassiveForm());
    }

    IEnumerator ReturnFromPassiveForm()
    {
        yield return new WaitForSeconds(passiveDuration);

        // กลับมาใช้ material ปกติ
        if (meshRenderer != null && normalMaterial != null)
            meshRenderer.material = normalMaterial;

        // ปิด PassiveForm และกลับ Idle
        if (animator != null)
        {
            if (animator.HasParameter("PassiveForm"))
                animator.SetBool("PassiveForm", false);

            animator.CrossFade("Idle", 0.1f);
        }

        isInPassiveForm = false;
    }
}

// ✨ Extension สำหรับเช็ค parameter ใน Animator
public static class AnimatorExtensions
{
    public static bool HasParameter(this Animator animator, string name)
    {
        foreach (var p in animator.parameters)
            if (p.name == name) return true;
        return false;
    }
}
