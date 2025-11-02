namespace AF.ECT.Shared.Enums;

/// <summary>
/// Represents user roles for authorization in the ECT system.
/// </summary>
public enum UserRole
{
    /// <summary>
    /// System administrator with full access.
    /// </summary>
    SysAdmin,

    /// <summary>
    /// LOD search permission.
    /// </summary>
    LodSearch,

    /// <summary>
    /// LOD create permission.
    /// </summary>
    LodCreate,

    /// <summary>
    /// View user's own LOD cases.
    /// </summary>
    MyLod,

    /// <summary>
    /// LOD view permission.
    /// </summary>
    LodView,

    /// <summary>
    /// View all LOD cases.
    /// </summary>
    LodViewAllCases,

    /// <summary>
    /// Reinvestigate permission.
    /// </summary>
    Reinvestigate,

    /// <summary>
    /// Reinvestigation search permission.
    /// </summary>
    ReinvestigateSearch,

    /// <summary>
    /// Execute post-completion tasks.
    /// </summary>
    ExePostCompletion,

    /// <summary>
    /// View user's own appeal cases.
    /// </summary>
    MyAP,

    /// <summary>
    /// Appeal search permission.
    /// </summary>
    APSearch,

    /// <summary>
    /// Appeal completion permission.
    /// </summary>
    APCompletion,

    /// <summary>
    /// Appeal view permission.
    /// </summary>
    APView,

    /// <summary>
    /// View user's own SARC cases.
    /// </summary>
    MyRSARC,

    /// <summary>
    /// SARC search permission.
    /// </summary>
    RSARCSearch,

    /// <summary>
    /// SARC post-completion permission.
    /// </summary>
    RSARCPostCompletion,

    /// <summary>
    /// SARC view permission.
    /// </summary>
    RSARCView,

    /// <summary>
    /// SARC create permission.
    /// </summary>
    RSARCCreate,

    /// <summary>
    /// SARC unrestricted create permission.
    /// </summary>
    SARCUnrestrictedCreate,

    /// <summary>
    /// View user's own SARC appeal cases.
    /// </summary>
    MyRSARCAppeal,

    /// <summary>
    /// SARC appeal search permission.
    /// </summary>
    RSARCAppealSearch,

    /// <summary>
    /// SARC appeal post-completion permission.
    /// </summary>
    RSARCAppealPostCompletion,

    /// <summary>
    /// SARC appeal view permission.
    /// </summary>
    RSARCAppealView,

    /// <summary>
    /// Reinvestigation request view permission.
    /// </summary>
    RRView,

    /// <summary>
    /// Special case search permission.
    /// </summary>
    ScSearch,

    /// <summary>
    /// Special case search MT permission.
    /// </summary>
    ScSearchMT,

    /// <summary>
    /// Special case create permission.
    /// </summary>
    ScCreate,

    /// <summary>
    /// View user's own special cases.
    /// </summary>
    MySCs,

    /// <summary>
    /// View user's own BMT cases.
    /// </summary>
    MyBmt,

    /// <summary>
    /// BMT create permission.
    /// </summary>
    BmtCreate,

    /// <summary>
    /// BMT search permission.
    /// </summary>
    BMTSearch,

    /// <summary>
    /// BMT view permission.
    /// </summary>
    BMTView,

    /// <summary>
    /// View user's own BCMR cases.
    /// </summary>
    MyBcmr,

    /// <summary>
    /// BCMR create permission.
    /// </summary>
    BcmrCreate,

    /// <summary>
    /// BCMR search permission.
    /// </summary>
    BCMRSearch,

    /// <summary>
    /// BCMR view permission.
    /// </summary>
    BCMRView,

    /// <summary>
    /// View user's own CMAS cases.
    /// </summary>
    MyCmas,

    /// <summary>
    /// CMAS create permission.
    /// </summary>
    CmasCreate,

    /// <summary>
    /// CMAS search permission.
    /// </summary>
    CMASSearch,

    /// <summary>
    /// CMAS view permission.
    /// </summary>
    CMASView,

    /// <summary>
    /// View user's own Congressional cases.
    /// </summary>
    MyCi,

    /// <summary>
    /// Congressional create permission.
    /// </summary>
    CiCreate,

    /// <summary>
    /// Congressional search permission.
    /// </summary>
    CISearch,

    /// <summary>
    /// Congressional view permission.
    /// </summary>
    CIView,

    /// <summary>
    /// View user's own Deployment Waiver cases.
    /// </summary>
    MyDW,

    /// <summary>
    /// Deployment Waiver create permission.
    /// </summary>
    DWCreate,

    /// <summary>
    /// Deployment Waiver search permission.
    /// </summary>
    DWSearch,

    /// <summary>
    /// Deployment Waiver view permission.
    /// </summary>
    DWView,

    /// <summary>
    /// View user's own INCAP cases.
    /// </summary>
    MyIncap,

    /// <summary>
    /// INCAP create permission.
    /// </summary>
    IncapCreate,

    /// <summary>
    /// INCAP search permission.
    /// </summary>
    INCAPSearch,

    /// <summary>
    /// INCAP view permission.
    /// </summary>
    INCAPView,

    /// <summary>
    /// View user's own IRILO cases.
    /// </summary>
    MyFTs,

    /// <summary>
    /// IRILO create permission.
    /// </summary>
    FTCreate,

    /// <summary>
    /// IRILO search permission.
    /// </summary>
    FTSearch,

    /// <summary>
    /// IRILO view permission.
    /// </summary>
    IRILOView,

    /// <summary>
    /// View user's own MEB cases.
    /// </summary>
    MyMeb,

    /// <summary>
    /// MEB create permission.
    /// </summary>
    MebCreate,

    /// <summary>
    /// MEB search permission.
    /// </summary>
    MEBSearch,

    /// <summary>
    /// MEB view permission.
    /// </summary>
    MEBView,

    /// <summary>
    /// View user's own Medical Hold cases.
    /// </summary>
    MyMH,

    /// <summary>
    /// Medical Hold create permission.
    /// </summary>
    MHCreate,

    /// <summary>
    /// Medical Hold search permission.
    /// </summary>
    MHSearch,

    /// <summary>
    /// Medical Hold view permission.
    /// </summary>
    MHView,

    /// <summary>
    /// View user's own Modification cases.
    /// </summary>
    MyMO,

    /// <summary>
    /// Modification create permission.
    /// </summary>
    MOCreate,

    /// <summary>
    /// Modification search permission.
    /// </summary>
    MOSearch,

    /// <summary>
    /// Modification view permission.
    /// </summary>
    MOView,

    /// <summary>
    /// View user's own Non-Emergent Surgery Request cases.
    /// </summary>
    MyNE,

    /// <summary>
    /// Non-Emergent Surgery Request create permission.
    /// </summary>
    NECreate,

    /// <summary>
    /// Non-Emergent Surgery Request search permission.
    /// </summary>
    NESearch,

    /// <summary>
    /// Non-Emergent Surgery Request view permission.
    /// </summary>
    NEView,

    /// <summary>
    /// View user's own Participation Waiver cases.
    /// </summary>
    MyPwaiver,

    /// <summary>
    /// Participation Waiver create permission.
    /// </summary>
    PwaiverCreate,

    /// <summary>
    /// Participation Waiver search permission.
    /// </summary>
    PWSearch,

    /// <summary>
    /// Participation Waiver view permission.
    /// </summary>
    PWView,

    /// <summary>
    /// View user's own AGR Certification cases.
    /// </summary>
    MyAGRCert,

    /// <summary>
    /// AGR Certification create permission.
    /// </summary>
    AGRCertCreate,

    /// <summary>
    /// AGR Certification search permission.
    /// </summary>
    AGRCertSearch,

    /// <summary>
    /// AGR Certification view permission.
    /// </summary>
    AGRCertView,

    /// <summary>
    /// View user's own PEPP/AIMWITS cases.
    /// </summary>
    MyPEPP,

    /// <summary>
    /// PEPP create permission.
    /// </summary>
    PEPPCreate,

    /// <summary>
    /// PEPP search permission.
    /// </summary>
    PEPPSearch,

    /// <summary>
    /// PEPP view permission.
    /// </summary>
    PEPPView,

    /// <summary>
    /// View user's own PH cases.
    /// </summary>
    MyPH,

    /// <summary>
    /// PH create permission.
    /// </summary>
    PHCreate,

    /// <summary>
    /// PH search permission.
    /// </summary>
    PHSearch,

    /// <summary>
    /// PH view permission.
    /// </summary>
    PHView,

    /// <summary>
    /// View user's own Recruiting Services cases.
    /// </summary>
    MyRS,

    /// <summary>
    /// Recruiting Services create permission.
    /// </summary>
    RSCreate,

    /// <summary>
    /// Recruiting Services search permission.
    /// </summary>
    RSSearch,

    /// <summary>
    /// Recruiting Services view permission.
    /// </summary>
    RSView,

    /// <summary>
    /// View user's own Retention Waiver cases.
    /// </summary>
    MyRW,

    /// <summary>
    /// Retention Waiver create permission.
    /// </summary>
    RWCreate,

    /// <summary>
    /// Retention Waiver search permission.
    /// </summary>
    RWSearch,

    /// <summary>
    /// Retention Waiver view permission.
    /// </summary>
    RWView,

    /// <summary>
    /// View user's own Worldwide Duty cases.
    /// </summary>
    MyWWDs,

    /// <summary>
    /// Worldwide Duty create permission.
    /// </summary>
    WWDCreate,

    /// <summary>
    /// Worldwide Duty search permission.
    /// </summary>
    WWDSearch,

    /// <summary>
    /// Worldwide Duty view permission.
    /// </summary>
    WWDView,

    /// <summary>
    /// View user's own PSCD cases.
    /// </summary>
    MyPSCDs,

    /// <summary>
    /// PSCD create permission.
    /// </summary>
    PSCDCreate,

    /// <summary>
    /// PSCD search permission.
    /// </summary>
    PSCDSearch,

    /// <summary>
    /// PSCD view permission.
    /// </summary>
    PSCDView,

    /// <summary>
    /// MMSO create permission.
    /// </summary>
    MMSOCreate,

    /// <summary>
    /// MMSO view permission.
    /// </summary>
    MMSOView,

    /// <summary>
    /// MMSO search permission.
    /// </summary>
    MMSOSearch,

    /// <summary>
    /// Ad-hoc report permission.
    /// </summary>
    AdHocReport,

    /// <summary>
    /// SARC ad-hoc reports permission.
    /// </summary>
    SARCAdHocReports,

    /// <summary>
    /// LOD category count report permission.
    /// </summary>
    LODCategoryCountReport,

    /// <summary>
    /// Disapproved LOD report permission.
    /// </summary>
    DisapprovedLODReport,

    /// <summary>
    /// Disposition reports permission.
    /// </summary>
    DispositionReports,

    /// <summary>
    /// LOD graph report permission.
    /// </summary>
    LODGraphReport,

    /// <summary>
    /// LOD metrics report cases permission.
    /// </summary>
    LODMetricsReport_Cases,

    /// <summary>
    /// LOD metrics report average permission.
    /// </summary>
    LODMetricsReport_Avg,

    /// <summary>
    /// LOD physician cancelled report permission.
    /// </summary>
    LODPhysicianCancelledReport,

    /// <summary>
    /// LOD compliance report permission.
    /// </summary>
    LODComplianceReport,

    /// <summary>
    /// LOD suspense monitoring report permission.
    /// </summary>
    LODSuspenseMonitoringReport,

    /// <summary>
    /// LOD program status report permission.
    /// </summary>
    LODProgramStatusReport,

    /// <summary>
    /// RWOA report permission.
    /// </summary>
    RWOAReport,

    /// <summary>
    /// LOD SARC cases report unrestricted permission.
    /// </summary>
    LODSARCCasesReportUnrestricted,

    /// <summary>
    /// LOD SARC cases report all permission.
    /// </summary>
    LODSARCCasesReportAll,

    /// <summary>
    /// LOD statistics report permission.
    /// </summary>
    LODStatisticsReport,

    /// <summary>
    /// LOD total count by process report permission.
    /// </summary>
    LODTotalCountByProcessReport,

    /// <summary>
    /// PAL documents report permission.
    /// </summary>
    PALDocumentsReport,

    /// <summary>
    /// PH totals report permission.
    /// </summary>
    PHTotalsReport,

    /// <summary>
    /// PWaiver report permission.
    /// </summary>
    PwaiverReport,

    /// <summary>
    /// RS disposition report permission.
    /// </summary>
    RSDispositionReport,

    /// <summary>
    /// RFA report by unit permission.
    /// </summary>
    RFAReportByUnit,

    /// <summary>
    /// RFA report by group permission.
    /// </summary>
    RFAReportByGroup,

    /// <summary>
    /// Users view permission.
    /// </summary>
    UsersView,

    /// <summary>
    /// Units edit permission.
    /// </summary>
    UnitsEdit,

    /// <summary>
    /// Users modify permission.
    /// </summary>
    UsersModify,

    /// <summary>
    /// Users approve permission.
    /// </summary>
    UsersApprove,

    /// <summary>
    /// Users edit permission.
    /// </summary>
    UsersEdit,

    /// <summary>
    /// Feedback manage permission.
    /// </summary>
    FeedbackManage,

    /// <summary>
    /// View user log permission.
    /// </summary>
    ViewUserLog,

    /// <summary>
    /// Member add permission.
    /// </summary>
    MemberAdd,

    /// <summary>
    /// Message admin permission.
    /// </summary>
    MsgAdmin,

    /// <summary>
    /// Digital signature permission.
    /// </summary>
    Signature,

    /// <summary>
    /// Report ad-hoc LOD permission.
    /// </summary>
    ReportAdHocLod,

    /// <summary>
    /// Report search soldier details permission.
    /// </summary>
    RepSearchSoldierDetails,

    /// <summary>
    /// Report search LOD details permission.
    /// </summary>
    RepSearchLODDetails,

    /// <summary>
    /// Report search time frame permission.
    /// </summary>
    RepSearchTimeFrame,

    /// <summary>
    /// Report search summary options permission.
    /// </summary>
    RepSearchSummaryOptions,

    /// <summary>
    /// Report search workload permission.
    /// </summary>
    RepSearchWorkload
}
