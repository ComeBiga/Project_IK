//Copyright(c) 2021 Razeware LLC

//Permission is hereby granted, free of charge, to any person
//obtaining a copy of this software and associated documentation
//files (the "Software"), to deal in the Software without
//restriction, including without limitation the rights to use,
//copy, modify, merge, publish, distribute, sublicense, and/or
//sell copies of the Software, and to permit persons to whom
//the Software is furnished to do so, subject to the following
//conditions:

//The above copyright notice and this permission notice shall be
//included in all copies or substantial portions of the Software.

//Notwithstanding the foregoing, you may not use, copy, modify,
//merge, publish, distribute, sublicense, create a derivative work,
//and/or sell copies of the Software in any work that is designed,
//intended, or marketed for pedagogical or instructional purposes
//related to programming, coding, application development, or
//information technology. Permission for such use, copying,
//modification, merger, publication, distribution, sublicensing,
//creation of derivative works, or sale is expressly withheld.

//This project and source code may use libraries or frameworks
//that are released under various Open-Source licenses. Use of
//those libraries and frameworks are governed by their own
//individual licenses.

//THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
//EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
//MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
//NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT
//HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
//WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
//OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
//DEALINGS IN THE SOFTWARE.

using UnityEngine;

/// <summary>
/// Simple day/night system
/// </summary>
[ExecuteInEditMode]
public class DayNightController : MonoBehaviour
{
    [Range(0, 1)]
    public float time = 0.5f; // the global 'time'

    public bool autoIcrement;
    public float speed = 1f;

    [Header("Skybox Settings")]
    public Material _skybox; // skybox reference
    public Gradient _skyboxColour; // skybox tint over time
    public AnimationCurve _skyboxIntensity; // skybox intensity over time

    [Header("Sun Settings")]
    public Light _sun; // sun light
    public Gradient _sunColour; // sun light colour over time
    [Range(0, 360)]
    public float _northHeading = 180; // north

    [Range(0, 90)] public float _tilt = 0f;

    // Fog
    [Header("Fog Settings")][GradientUsage(true)]
    public Gradient _fogColour; // fog colour over time

    // vars
    private float _prevTime; // previous time

    private void Awake()
    {
        SetTimeOfDay(time);
        _prevTime = time;
    }

    // Update is called once per frame
    private void Update()
    {
        if (autoIcrement)
        {
            var t = Mathf.PingPong(Time.time * speed, 0.4f - 0.16f) + 0.16f;
            time = t;// * 0.5f + 0.25f;
        }

        if (time != _prevTime) // if time has changed
        {
            SetTimeOfDay(time);
        }
    }

    /// <summary>
    /// Sets the time of day
    /// </summary>
    /// <param name="time">Time in linear 0-1</param>
    public void SetTimeOfDay(float time)
    {
        this.time = time;
        _prevTime = time;

        // do update
        if (_sun)
        {
            // update sun
            _sun.transform.forward = Vector3.down;
            _sun.transform.rotation *= Quaternion.AngleAxis(_northHeading, Vector3.forward); // north facing
            _sun.transform.rotation *= Quaternion.AngleAxis(_tilt, Vector3.up);
            _sun.transform.rotation *= Quaternion.AngleAxis((this.time * 360f) - 180f, Vector3.right); // time of day

            _sun.color = _sunColour.Evaluate(TimeToGradient(this.time));
        }
        if (_skybox)
        {
            // update skybox
            _skybox.SetFloat("_Rotation", 85 + ((this.time - 0.5f) * 20f)); // rotate slightly for cheap moving cloud eefect
            _skybox.SetColor("_Tint", _skyboxColour.Evaluate(TimeToGradient(this.time)));
        }

        RenderSettings.fogColor = _fogColour.Evaluate(TimeToGradient(this.time)); // update fog colour
        RenderSettings.ambientIntensity = _skyboxIntensity.Evaluate(this.time); // update ambient light colour
    }

    private float TimeToGradient(float time)
    {
        return Mathf.Abs(time * 2f - 1f);
    }
}