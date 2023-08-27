//HintName: EvaluateMeasureOperation.g.cs
#nullable enable

/// <summary>
/// Evaluate Measure
/// The evaluate-measure operation is used to calculate an eMeasure and obtain the results
/// </summary>
public class EvaluateMeasureOperation
{
    /// <summary>
    /// Name used to invoke the operation.
    /// </summary>
    public const string Name = "evaluate-measure";

    /// <summary>
    /// The operation's request parameters.
    /// </summary>
    public class Request
    {
        /// <summary>
        /// The start of the measurement period. In keeping with the semantics of the date parameter used in the FHIR search operation, the period will start at the beginning of the period implied by the supplied timestamp. E.g. a value of 2014 would set the period start to be 2014-01-01T00:00:00 inclusive
        /// </summary>
        public Hl7.Fhir.Model.Date PeriodStart { get; set; }
        /// <summary>
        /// The end of the measurement period. The period will end at the end of the period implied by the supplied timestamp. E.g. a value of 2014 would set the period end to be 2014-12-31T23:59:59 inclusive
        /// </summary>
        public Hl7.Fhir.Model.Date PeriodEnd { get; set; }
        /// <summary>
        /// The measure to evaluate. This parameter is only required when the operation is invoked on the resource type, it is not used when invoking the operation on a Measure instance
        /// </summary>
        public Hl7.Fhir.Model.String? Measure { get; set; }
        /// <summary>
        /// The type of measure report: subject, subject-list, or population. If not specified, a default value of subject will be used if the subject parameter is supplied, otherwise, population will be used
        /// </summary>
        public Hl7.Fhir.Model.Code? ReportType { get; set; }
        /// <summary>
        /// Subject for which the measure will be calculated. If not specified, the measure will be calculated for all subjects that meet the requirements of the measure. If specified, the measure will only be calculated for the referenced subject(s)
        /// </summary>
        public Hl7.Fhir.Model.String? Subject { get; set; }
        /// <summary>
        /// Practitioner for which the measure will be calculated. If specified, the measure will be calculated only for subjects that have a primary relationship to the identified practitioner
        /// </summary>
        public Hl7.Fhir.Model.String? Practitioner { get; set; }
        /// <summary>
        /// The date the results of this measure were last received. This parameter is only valid for patient-level reports and is used to indicate when the last time a result for this patient was received. This information can be used to limit the set of resources returned for a patient-level report
        /// </summary>
        public Hl7.Fhir.Model.DateTime? LastReceivedOn { get; set; }

        /// <summary>
        /// Convert this object to its FHIR Parameters representation.
        /// </summary>
        /// <returns>A FHIR Parameters instance.</returns>
        public Hl7.Fhir.Model.Parameters ToParameters()
        {
            var parameters = new Parameters();

            parameters.Add("periodStart", this.PeriodStart);
            parameters.Add("periodEnd", this.PeriodEnd);
            parameters.Add("measure", this.Measure);
            parameters.Add("reportType", this.ReportType);
            parameters.Add("subject", this.Subject);
            parameters.Add("practitioner", this.Practitioner);
            parameters.Add("lastReceivedOn", this.LastReceivedOn);

            return parameters;
        }
    }

    /// <summary>
    /// The operation's response parameters.
    /// </summary>
    public class Response
    {
        /// <summary>
        /// The results of the measure calculation. See the MeasureReport resource for a complete description of the output of this operation. Note that implementations may choose to return a MeasureReport with a status of pending to indicate that the report is still being generated. In this case, the client can use a polling method to continually request the MeasureReport until the status is updated to complete
        /// </summary>
        public Hl7.Fhir.Model.MeasureReport Return { get; set; }

        /// <summary>
        /// Convert this object to its FHIR Parameters representation.
        /// </summary>
        /// <returns>A FHIR Parameters instance.</returns>
        public Hl7.Fhir.Model.Parameters ToParameters()
        {
            var parameters = new Parameters();

            parameters.Add("return", this.Return);

            return parameters;
        }
    }
}
