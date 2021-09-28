using System;
using System.Reflection;
using UnityEditor.ShaderGraph.Drawing;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.ShaderGraph.Drawing.Inspector.PropertyDrawers
{
    [SGPropertyDrawer(typeof(Matrix4x4))]
    class MatrixPropertyDrawer : IPropertyDrawer
    {
        public enum MatrixDimensions
        {
            Two,
            Three,
            Four
        }
        public MatrixDimensions dimension { get; set; }

        public delegate Vector4 GetMatrixRowDelegate(int rowNumber);

        internal Action PreValueChangeCallback;
        internal delegate void ValueChangedCallback(Matrix4x4 newValue);
        internal Action PostValueChangeCallback;
        // Matrix4x4, Matrix3x3, Matrix2x2 are all value types,
        // hence the local value doesn't stay up to date after modified
        // Need a callback to fetch the row data directly from the source
        internal GetMatrixRowDelegate MatrixRowFetchCallback;

        void HandleMatrix2Property(
            ValueChangedCallback valueChangedCallback,
            PropertySheet propertySheet,
            Matrix4x4 matrix2Property,
            string labelName = "Default")
        {
            var vector2PropertyDrawer = new Vector2PropertyDrawer();
            vector2PropertyDrawer.preValueChangeCallback = PreValueChangeCallback;
            vector2PropertyDrawer.postValueChangeCallback = PostValueChangeCallback;

            propertySheet.Add(vector2PropertyDrawer.CreateGUI(
                newValue =>
                {
                    Vector2 row1 = MatrixRowFetchCallback(1);
                    valueChangedCallback(new Matrix4x4()
                    {
                        m00 = newValue.x,
                        m01 = newValue.y,
                        m02 = 0,
                        m03 = 0,
                        m10 = row1.x,
                        m11 = row1.y,
                        m12 = 0,
                        m13 = 0,
                        m20 = 0,
                        m21 = 0,
                        m22 = 0,
                        m23 = 0,
                        m30 = 0,
                        m31 = 0,
                        m32 = 0,
                        m33 = 0,
                    });
                },
                matrix2Property.GetRow(0),
                labelName,
                out var row0Field
            ));

            propertySheet.Add(vector2PropertyDrawer.CreateGUI(
                newValue =>
                {
                    Vector2 row0 = MatrixRowFetchCallback(0);
                    valueChangedCallback(new Matrix4x4()
                    {
                        m00 = row0.x,
                        m01 = row0.y,
                        m02 = 0,
                        m03 = 0,
                        m10 = newValue.x,
                        m11 = newValue.y,
                        m12 = 0,
                        m13 = 0,
                        m20 = 0,
                        m21 = 0,
                        m22 = 0,
                        m23 = 0,
                        m30 = 0,
                        m31 = 0,
                        m32 = 0,
                        m33 = 0,
                    });
                },
                matrix2Property.GetRow(1),
                "",
                out var row1Field
            ));
        }

        void HandleMatrix3Property(
            ValueChangedCallback valueChangedCallback,
            PropertySheet propertySheet,
            Matrix4x4 matrix3Property,
            string labelName = "Default")
        {
            var vector3PropertyDrawer = new Vector3PropertyDrawer();
            vector3PropertyDrawer.preValueChangeCallback = PreValueChangeCallback;
            vector3PropertyDrawer.postValueChangeCallback = PostValueChangeCallback;

            propertySheet.Add(vector3PropertyDrawer.CreateGUI(
                newValue =>
                {
                    Vector3 row1 = MatrixRowFetchCallback(1);
                    Vector3 row2 = MatrixRowFetchCallback(2);
                    valueChangedCallback(new Matrix4x4()
                    {
                        m00 = newValue.x,
                        m01 = newValue.y,
                        m02 = newValue.z,
                        m03 = 0,
                        m10 = row1.x,
                        m11 = row1.y,
                        m12 = row1.z,
                        m13 = 0,
                        m20 = row2.x,
                        m21 = row2.y,
                        m22 = row2.z,
                        m23 = 0,
                        m30 = 0,
                        m31 = 0,
                        m32 = 0,
                        m33 = 0,
                    });
                },
                matrix3Property.GetRow(0),
                labelName,
                out var row0Field
            ));

            propertySheet.Add(vector3PropertyDrawer.CreateGUI(
                newValue =>
                {
                    Vector3 row0 = MatrixRowFetchCallback(0);
                    Vector3 row2 = MatrixRowFetchCallback(2);
                    valueChangedCallback(new Matrix4x4()
                    {
                        m00 = row0.x,
                        m01 = row0.y,
                        m02 = row0.z,
                        m03 = 0,
                        m10 = newValue.x,
                        m11 = newValue.y,
                        m12 = newValue.z,
                        m13 = 0,
                        m20 = row2.x,
                        m21 = row2.y,
                        m22 = row2.z,
                        m23 = 0,
                        m30 = 0,
                        m31 = 0,
                        m32 = 0,
                        m33 = 0,
                    });
                },
                matrix3Property.GetRow(1),
                "",
                out var row1Field
            ));

            propertySheet.Add(vector3PropertyDrawer.CreateGUI(
                newValue =>
                {
                    Vector3 row0 = MatrixRowFetchCallback(0);
                    Vector3 row1 = MatrixRowFetchCallback(1);
                    valueChangedCallback(new Matrix4x4()
                    {
                        m00 = row0.x,
                        m01 = row0.y,
                        m02 = row0.z,
                        m03 = 0,
                        m10 = row1.x,
                        m11 = row1.y,
                        m12 = row1.z,
                        m13 = 0,
                        m20 = newValue.x,
                        m21 = newValue.y,
                        m22 = newValue.z,
                        m23 = 0,
                        m30 = 0,
                        m31 = 0,
                        m32 = 0,
                        m33 = 0,
                    });
                },
                matrix3Property.GetRow(2),
                "",
                out var row2Field
            ));
        }

        void HandleMatrix4Property(
            ValueChangedCallback valueChangedCallback,
            PropertySheet propertySheet,
            Matrix4x4 matrix4Property,
            string labelName = "Default")
        {
            var vector4PropertyDrawer = new Vector4PropertyDrawer();
            vector4PropertyDrawer.preValueChangeCallback = PreValueChangeCallback;
            vector4PropertyDrawer.postValueChangeCallback = PostValueChangeCallback;

            propertySheet.Add(vector4PropertyDrawer.CreateGUI(
                newValue =>
                {
                    Vector4 row1 = MatrixRowFetchCallback(1);
                    Vector4 row2 = MatrixRowFetchCallback(2);
                    Vector4 row3 = MatrixRowFetchCallback(3);
                    valueChangedCallback(new Matrix4x4()
                    {
                        m00 = newValue.x,
                        m01 = newValue.y,
                        m02 = newValue.z,
                        m03 = newValue.w,
                        m10 = row1.x,
                        m11 = row1.y,
                        m12 = row1.z,
                        m13 = row1.w,
                        m20 = row2.x,
                        m21 = row2.y,
                        m22 = row2.z,
                        m23 = row2.w,
                        m30 = row3.x,
                        m31 = row3.y,
                        m32 = row3.z,
                        m33 = row3.w,
                    });
                },
                matrix4Property.GetRow(0),
                labelName,
                out var row0Field
            ));

            propertySheet.Add(vector4PropertyDrawer.CreateGUI(
                newValue =>
                {
                    Vector4 row0 = MatrixRowFetchCallback(0);
                    Vector4 row2 = MatrixRowFetchCallback(2);
                    Vector4 row3 = MatrixRowFetchCallback(3);
                    valueChangedCallback(new Matrix4x4()
                    {
                        m00 = row0.x,
                        m01 = row0.y,
                        m02 = row0.z,
                        m03 = row0.w,
                        m10 = newValue.x,
                        m11 = newValue.y,
                        m12 = newValue.z,
                        m13 = newValue.w,
                        m20 = row2.x,
                        m21 = row2.y,
                        m22 = row2.z,
                        m23 = row2.w,
                        m30 = row3.x,
                        m31 = row3.y,
                        m32 = row3.z,
                        m33 = row3.w,
                    });
                },
                matrix4Property.GetRow(1),
                "",
                out var row1Field
            ));

            propertySheet.Add(vector4PropertyDrawer.CreateGUI(
                newValue =>
                {
                    Vector4 row0 = MatrixRowFetchCallback(0);
                    Vector4 row1 = MatrixRowFetchCallback(1);
                    Vector4 row3 = MatrixRowFetchCallback(3);
                    valueChangedCallback(new Matrix4x4()
                    {
                        m00 = row0.x,
                        m01 = row0.y,
                        m02 = row0.z,
                        m03 = row0.w,
                        m10 = row1.x,
                        m11 = row1.y,
                        m12 = row1.z,
                        m13 = row1.w,
                        m20 = newValue.x,
                        m21 = newValue.y,
                        m22 = newValue.z,
                        m23 = newValue.w,
                        m30 = row3.x,
                        m31 = row3.y,
                        m32 = row3.z,
                        m33 = row3.w,
                    });
                },
                matrix4Property.GetRow(2),
                "",
                out var row2Field));

            propertySheet.Add(vector4PropertyDrawer.CreateGUI(
                newValue =>
                {
                    Vector4 row0 = MatrixRowFetchCallback(0);
                    Vector4 row1 = MatrixRowFetchCallback(1);
                    Vector4 row2 = MatrixRowFetchCallback(2);
                    valueChangedCallback(new Matrix4x4()
                    {
                        m00 = row0.x,
                        m01 = row0.y,
                        m02 = row0.z,
                        m03 = row0.w,
                        m10 = row1.x,
                        m11 = row1.y,
                        m12 = row1.z,
                        m13 = row1.w,
                        m20 = row2.x,
                        m21 = row2.y,
                        m22 = row2.z,
                        m23 = row2.w,
                        m30 = newValue.x,
                        m31 = newValue.y,
                        m32 = newValue.z,
                        m33 = newValue.w,
                    });
                },
                matrix4Property.GetRow(3),
                "",
                out var row3Field
            ));
        }

        internal VisualElement CreateGUI(
            ValueChangedCallback valueChangedCallback,
            Matrix4x4 fieldToDraw,
            string labelName,
            out VisualElement propertyMatrixField,
            int indentLevel = 0)
        {
            var propertySheet = new PropertySheet();

            switch (dimension)
            {
                case MatrixDimensions.Two:
                    HandleMatrix2Property(valueChangedCallback, propertySheet, fieldToDraw, labelName);
                    break;
                case MatrixDimensions.Three:
                    HandleMatrix3Property(valueChangedCallback, propertySheet, fieldToDraw, labelName);
                    break;
                case MatrixDimensions.Four:
                    HandleMatrix4Property(valueChangedCallback, propertySheet, fieldToDraw, labelName);
                    break;
            }

            propertyMatrixField = propertySheet;
            return propertyMatrixField;
        }

        public Action inspectorUpdateDelegate { get; set; }

        public VisualElement DrawProperty(PropertyInfo propertyInfo, object actualObject, InspectableAttribute attribute)
        {
            return this.CreateGUI(
                // Use the setter from the provided property as the callback
                newValue => propertyInfo.GetSetMethod(true).Invoke(actualObject, new object[] { newValue }),
                (Matrix4x4)propertyInfo.GetValue(actualObject),
                attribute.labelName,
                out var propertyVisualElement);
        }
    }
}
