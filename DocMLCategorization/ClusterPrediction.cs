// Licensed under the MIT License. See LICENSE in the repository root for license information.

using Microsoft.ML.Data;

namespace DocMLCategorization
{
    /// <summary>
    /// Represents a prediction.
    /// </summary>
    public class ClusterPrediction
    {
        /// <summary>
        /// Score label.
        /// </summary>
        public const string Score = nameof(Score);

        /// <summary>
        /// Predicted label, label.
        /// </summary>
        public const string PredictedLabel = nameof(PredictedLabel);

        /// <summary>
        /// The category.
        /// </summary>
        [ColumnName(PredictedLabel)]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1401:Fields should be private", Justification = "Ambient references.")]
        public uint PredictedClusterId;

        /// <summary>
        /// The scores.
        /// </summary>
        [ColumnName(Score)]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1401:Fields should be private", Justification = "Ambient references.")]
        public float[] Distances;
    }
}
