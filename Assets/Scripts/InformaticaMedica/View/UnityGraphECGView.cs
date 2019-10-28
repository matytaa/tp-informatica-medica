using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace InformaticaMedica.View
{
    public class UnityGraphECGView : MonoBehaviour
    {
        [SerializeField] private Sprite circleSprite;
        private RectTransform graphContainer;
        

        private void Awake()
        {
            graphContainer = transform.Find("GraphContainer").GetComponent<RectTransform>();
            var points = new List<int>{5,98,56,45,30,22,17,25,37,40,36,33};
            ShowGraph(points);
        }

        private GameObject CreateCircule(Vector2 anchoredPosition)
        {
            var gameObject = new GameObject("circle", typeof(Image));
            gameObject.transform.SetParent(graphContainer, false);
            gameObject.GetComponent<Image>().sprite = circleSprite;
            var rectTransform = gameObject.GetComponent<RectTransform>();
            rectTransform.anchoredPosition = anchoredPosition;
            rectTransform.sizeDelta = new Vector2(5, 5);
            rectTransform.anchorMin = new Vector2(0, 0);
            rectTransform.anchorMax = new Vector2(0, 0);
            return gameObject;
        }

        private void ShowGraph(List<int> points)
        {
            var graphHeight = graphContainer.sizeDelta.y;
            var yMaximun = 100f;
            var xSize = 25f;
            GameObject lastCircle = null;
            
            for (var point = 0; point < points.Count - 1; point++)
            {
                var initialPoint = points[point];
                var finalPoint = points[point+1];
                while (Math.Abs(initialPoint - finalPoint) > 0)
                {
                    var column = initialPoint * xSize;
                    var row = (points[initialPoint] / yMaximun) * graphHeight;
                    GameObject circle = CreateCircule(new Vector2(column,row));

                    if (lastCircle != null)
                        DotConnection(lastCircle.GetComponent<RectTransform>().anchoredPosition,
                            circle.GetComponent<RectTransform>().anchoredPosition);
                    lastCircle = circle;

                    if (initialPoint < finalPoint)
                        initialPoint++;
                    else
                        initialPoint--;
                }
            }
        }

        private void DotConnection(Vector2 origin, Vector2 destiny)
        {
            /*
            var gameObject = new GameObject("dotConnection", typeof(Image));
            gameObject.transform.SetParent(graphContainer, false);
            gameObject.GetComponent<Image>().color = new Color(1,1,1, 0.5f);
            var rectTransform = gameObject.GetComponent<RectTransform>();
            var direction = (destiny - origin).normalized;
            var distance = Vector2.Distance(origin, destiny);
            rectTransform.anchorMin = new Vector2(0, 0);
            rectTransform.anchorMax = new Vector2(0, 0);
            rectTransform.sizeDelta = new Vector2(distance, 3f);
            rectTransform.anchoredPosition = origin + direction * distance * 0.5f;
            rectTransform.localEulerAngles = new Vector3 (0,0, Vector2.Angle(destiny, origin));
            */
            var lineRenderer = GetComponent<LineRenderer>();
            lineRenderer.positionCount = 2;
            lineRenderer.SetPosition(0, new Vector3(origin.x, origin.y, 0f));
            lineRenderer.SetPosition(1, new Vector3(destiny.x, destiny.y, 0f));
        }
    }
}