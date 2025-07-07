using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IKFootSolver : MonoBehaviour
{
    [SerializeField] LayerMask terrainLayer = default;
    [SerializeField] Transform body = default;
    [SerializeField] IKFootSolver otherFoot = default; // 반대쪽 발
    [SerializeField] float speed = 3; // 발을 얼마나 빨리 당겨오는지
    [SerializeField] float stepDistance = 0.2f; // 당겨오는 거리
    [SerializeField] float stepLength = 0.2f; // 보폭
    [SerializeField] float stepHeight = 0.2f; // 무릎이 올라오는 높이 
    [SerializeField] Vector3 footOffset = default; // 기본 발 포지션 오프셋
    [SerializeField] private Vector3 footRotOffset; // 발이 꺾이지않게 잡아주는 오프셋 
    float footSpacing;
    Vector3 oldPosition, currentPosition, newPosition;
    Vector3 oldNormal, currentNormal, newNormal;
    private float lerp = 1;

    private void Start()
    {
        footSpacing = transform.localPosition.x;
        currentPosition = newPosition = oldPosition = body.forward * stepLength;
        currentNormal = newNormal = oldNormal = transform.up;
    }

    private void Update()
    {
        this.transform.position = currentPosition + footOffset;
        var newRot = footRotOffset + (body.forward * 90f);
        this.transform.rotation = Quaternion.Lerp(this.transform.rotation, Quaternion.Euler(newRot), 0.1f);

        Ray ray = new Ray(body.position + (body.right * footSpacing) + (Vector3.up * 2), Vector3.down);

        if (Physics.Raycast(ray, out RaycastHit info, 10, terrainLayer.value))
        {
            if (Vector3.Distance(newPosition, info.point) > stepDistance && !otherFoot.IsMoving() && lerp >= 1)
            {
                lerp = 0;
                int direction = body.InverseTransformPoint(info.point).z > body.InverseTransformPoint(newPosition).z ? 1 : -1;
                newPosition = info.point + (body.forward * stepLength * direction);
                newNormal = info.normal;
                Debug.Log(newPosition);
            }
        }

        if (lerp < 1)
        {
            Vector3 tempPosition = Vector3.Lerp(oldPosition, newPosition, lerp);
            tempPosition.y += Mathf.Sin(lerp * Mathf.PI) * stepHeight;

            currentPosition = tempPosition; 
            currentNormal = Vector3.Lerp(oldNormal, newNormal, lerp);
            lerp += Time.deltaTime * speed;
        }
        else
        {
            oldPosition = newPosition;
            oldNormal = newNormal;  
        }
    }
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawSphere(newPosition, 0.5f);
    }
    public bool IsMoving()
    {
        return lerp < 1;
    }
}
