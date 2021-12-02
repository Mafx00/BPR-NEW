using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class UIRayCaster : MonoBehaviour
{
    float m_LineWidth = 0.02f;
    public float lineWidth;

    public bool overrideInteractorLineLength;

    [SerializeField]
    public float lineLength;

    public Gradient validColorGradient;

    public bool smoothMovement;

    public float followTightness;

    public float snapThresholdDistance;

    public GameObject reticle;

    public bool stopLineAtFirstRaycastHit;

    Vector3 m_ReticlePos;
    Vector3 m_ReticleNormal;
    int m_EndPositionInLine;

    bool m_SnapCurve = true;
    bool m_PerformSetup;
    GameObject m_ReticleToUse;

    LineRenderer m_LineRenderer;

    // interface to get target point
    ILineRenderable m_LineRenderable;

    // reusable lists of target points
    Vector3[] m_TargetPoints;
    int m_NoTargetPoints = -1;

    // reusable lists of rendered points
    Vector3[] m_RenderPoints;
    int m_NoRenderPoints = -1;

    // reusable lists of rendered points to smooth movement
    Vector3[] m_PreviousRenderPoints;
    int m_NoPreviousRenderPoints = -1;

    readonly Vector3[] m_ClearArray = { Vector3.zero, Vector3.zero };

    GameObject m_CustomReticle;
    bool m_CustomReticleAttached;

    /// <summary>
    /// See <see cref="MonoBehaviour"/>.
    /// </summary>
    protected void Reset()
    {
        if (TryFindLineRenderer())
        {
            ClearLineRenderer();
            UpdateSettings();
        }
    }

    /// <summary>
    /// See <see cref="MonoBehaviour"/>.
    /// </summary>
    protected void OnValidate()
    {
        UpdateSettings();
    }

    /// <summary>
    /// See <see cref="MonoBehaviour"/>.
    /// </summary>
    protected void Awake()
    {
        m_LineRenderable = GetComponent<ILineRenderable>();

        if (reticle != null)
            reticle.SetActive(false);

        UpdateSettings();
    }

    /// <summary>
    /// See <see cref="MonoBehaviour"/>.
    /// </summary>
    protected void OnEnable()
    {
        m_SnapCurve = true;
        m_ReticleToUse = null;

        Reset();

        Application.onBeforeRender += OnBeforeRenderLineVisual;
    }

    /// <summary>
    /// See <see cref="MonoBehaviour"/>.
    /// </summary>
    protected void OnDisable()
    {
        if (m_LineRenderer != null)
            m_LineRenderer.enabled = false;
        m_ReticleToUse = null;

        Application.onBeforeRender -= OnBeforeRenderLineVisual;
    }

    void ClearLineRenderer()
    {
        if (TryFindLineRenderer())
        {
            m_LineRenderer.SetPositions(m_ClearArray);
            m_LineRenderer.positionCount = 0;
        }
    }

    [BeforeRenderOrder(XRInteractionUpdateOrder.k_BeforeRenderLineVisual)]
    void OnBeforeRenderLineVisual()
    {
        UpdateLineVisual();
    }

    void UpdateLineVisual()
    {
        if (m_PerformSetup)
        {
            UpdateSettings();
            m_PerformSetup = false;
        }
        if (m_LineRenderer == null)
            return;

        m_NoRenderPoints = 0;

        // Get all the line sample points from the ILineRenderable interface
        if (!m_LineRenderable.GetLinePoints(ref m_TargetPoints, out m_NoTargetPoints))
        {
            m_LineRenderer.enabled = false;
            ClearLineRenderer();
            return;
        }

        // Sanity check.
        if (m_TargetPoints == null ||
            m_TargetPoints.Length == 0 ||
            m_NoTargetPoints == 0 ||
            m_NoTargetPoints > m_TargetPoints.Length)
        {
            m_LineRenderer.enabled = false;
            ClearLineRenderer();
            return;
        }

        // Make sure we have the correct sized arrays for everything.
        if (m_RenderPoints == null || m_RenderPoints.Length < m_NoTargetPoints)
        {
            m_RenderPoints = new Vector3[m_NoTargetPoints];
            m_PreviousRenderPoints = new Vector3[m_NoTargetPoints];
            m_NoRenderPoints = 0;
            m_NoPreviousRenderPoints = 0;
        }

        // If there is a big movement (snap turn, teleportation), snap the curve
        if (m_PreviousRenderPoints.Length != m_NoTargetPoints)
        {
            m_SnapCurve = true;
        }
        else
        {
            // Compare the two endpoints of the curve, as that will have the largest delta.
            if (m_PreviousRenderPoints != null &&
                m_NoPreviousRenderPoints > 0 &&
                m_NoPreviousRenderPoints <= m_PreviousRenderPoints.Length &&
                m_TargetPoints != null &&
                m_NoTargetPoints > 0 &&
                m_NoTargetPoints <= m_TargetPoints.Length)
            {
                var prevPointIndex = m_NoPreviousRenderPoints - 1;
                var currPointIndex = m_NoTargetPoints - 1;
                if (Vector3.Distance(m_PreviousRenderPoints[prevPointIndex], m_TargetPoints[currPointIndex]) > snapThresholdDistance)
                {
                    m_SnapCurve = true;
                }
            }
        }

        if (m_LineRenderable.TryGetHitInfo(out m_ReticlePos, out m_ReticleNormal, out m_EndPositionInLine, out var isValidTarget))
        {
            // End the line at the current hit point.
            if ((isValidTarget || stopLineAtFirstRaycastHit) && m_EndPositionInLine > 0 && m_EndPositionInLine < m_NoTargetPoints)
            {
                m_TargetPoints[m_EndPositionInLine] = m_ReticlePos;
                m_NoTargetPoints = m_EndPositionInLine + 1;
            }
        }
        if (smoothMovement && (m_NoPreviousRenderPoints == m_NoTargetPoints) && !m_SnapCurve)
        {
            // Smooth movement by having render points follow target points
            var length = 0f;
            var maxRenderPoints = m_RenderPoints.Length;
            for (var i = 0; i < m_NoTargetPoints && m_NoRenderPoints < maxRenderPoints; ++i)
            {
                var smoothPoint = Vector3.Lerp(m_PreviousRenderPoints[i], m_TargetPoints[i], followTightness * Time.deltaTime);

                if (overrideInteractorLineLength)
                {
                    if (m_NoRenderPoints > 0 && m_RenderPoints.Length > 0)
                    {
                        var segLength = Vector3.Distance(m_RenderPoints[m_NoRenderPoints - 1], smoothPoint);
                        length += segLength;
                        if (length > lineLength)
                        {
                            var delta = length - lineLength;
                            // Re-project final point to match the desired length
                            smoothPoint = Vector3.Lerp(m_RenderPoints[m_NoRenderPoints - 1], smoothPoint, delta / segLength);
                            m_RenderPoints[m_NoRenderPoints] = smoothPoint;
                            m_NoRenderPoints++;
                            break;
                        }
                    }

                    m_RenderPoints[m_NoRenderPoints] = smoothPoint;
                    m_NoRenderPoints++;
                }
                else
                {
                    m_RenderPoints[m_NoRenderPoints] = smoothPoint;
                    m_NoRenderPoints++;
                }
            }
        }
        else
        {
            if (overrideInteractorLineLength)
            {
                var length = 0f;
                var maxRenderPoints = m_RenderPoints.Length;
                for (var i = 0; i < m_NoTargetPoints && m_NoRenderPoints < maxRenderPoints; ++i)
                {
                    var newPoint = m_TargetPoints[i];
                    if (m_NoRenderPoints > 0 && m_RenderPoints.Length > 0)
                    {
                        var segLength = Vector3.Distance(m_RenderPoints[m_NoRenderPoints - 1], newPoint);
                        length += segLength;
                        if (length > lineLength)
                        {
                            var delta = length - lineLength;
                            // Re-project final point to match the desired length
                            var resolvedPoint = Vector3.Lerp(m_RenderPoints[m_NoRenderPoints - 1], newPoint, 1 - (delta / segLength));
                            m_RenderPoints[m_NoRenderPoints] = resolvedPoint;
                            m_NoRenderPoints++;
                            break;
                        }
                    }

                    m_RenderPoints[m_NoRenderPoints] = newPoint;
                    m_NoRenderPoints++;
                }
            }
            else
            {
                Array.Copy(m_TargetPoints, m_RenderPoints, m_NoTargetPoints);
                m_NoRenderPoints = m_NoTargetPoints;
            }
        }

        // When a straight line has only two points and color gradients have more than two keys,
        // interpolate points between the two points to enable better color gradient effects.
        if (isValidTarget)
        {
            m_LineRenderer.enabled = true;
            m_LineRenderer.colorGradient = validColorGradient;
            // Set reticle position and show reticle
            m_ReticleToUse = m_CustomReticleAttached ? m_CustomReticle : reticle;
            if (m_ReticleToUse != null)
            {
                m_ReticleToUse.transform.position = m_ReticlePos;
                m_ReticleToUse.transform.up = m_ReticleNormal;
                m_ReticleToUse.SetActive(true);
            }
        }
        else
        {
            m_LineRenderer.enabled = false;
        }

        if (m_NoRenderPoints >= 2)
        {
           // m_LineRenderer.enabled = true;
            m_LineRenderer.positionCount = m_NoRenderPoints;
            m_LineRenderer.SetPositions(m_RenderPoints);
        }
        else
        {
            m_LineRenderer.enabled = false;
            ClearLineRenderer();
            return;
        }

        // Update previous points
        Array.Copy(m_RenderPoints, m_PreviousRenderPoints, m_NoRenderPoints);
        m_NoPreviousRenderPoints = m_NoRenderPoints;
        m_SnapCurve = false;
        m_ReticleToUse = null;
    }

    void UpdateSettings()
    {
        if (TryFindLineRenderer())
        {
            m_LineRenderer.widthMultiplier = lineWidth;
            //m_LineRenderer.widthCurve = m_WidthCurve;
            m_SnapCurve = true;
        }
    }

    bool TryFindLineRenderer()
    {
        m_LineRenderer = GetComponent<LineRenderer>();
        if (m_LineRenderer == null)
        {
            Debug.LogWarning("No Line Renderer found for Interactor Line Visual.", this);
            enabled = false;
            return false;
        }
        return true;
    }
}

   /* public LineRenderer lineRenderer;
    public XRInteractorLineVisual lineVisual;

    public Material transparentRay;
    public Material ray;

    // Start is called before the first frame update
   void Start()
    {

    }

    // Update is called once per frame
    void LateUpdate()
    {
        //rayInteractor.TryGetHitInfo();
       if (lineRenderer.colorGradient.colorKeys[0].Equals(lineVisual.validColorGradient.colorKeys[0]))
       {
            lineRenderer.material = ray;
            //lineRenderer.enabled = true;
            Debug.Log("hovering");
       }
       else
        {
            lineRenderer.material = transparentRay;
            Debug.Log("hovering not " + lineRenderer.colorGradient);

        }
    }*/


