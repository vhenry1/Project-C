using Unity.Netcode;
using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Follows the local player. Uses the Pixel Perfect Camera's RoundToPixel so the camera never lands
/// between pixels (that is what makes pixel art shimmer). Added to the Farm camera at runtime.
/// </summary>
public class CameraFollow2D : MonoBehaviour
{
    private PixelPerfectCamera pixelPerfect;

    private void Awake()
    {
        pixelPerfect = GetComponent<PixelPerfectCamera>();
    }

    private void LateUpdate()
    {
        NetworkManager nm = NetworkManager.Singleton;
        if (nm == null || nm.LocalClient == null || nm.LocalClient.PlayerObject == null) return;

        Vector3 target = nm.LocalClient.PlayerObject.transform.position;
        target.z = transform.position.z;
        transform.position = pixelPerfect != null ? pixelPerfect.RoundToPixel(target) : target;
    }
}
