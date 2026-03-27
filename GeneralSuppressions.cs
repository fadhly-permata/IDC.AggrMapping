/*
    This file contains global code analysis suppressions for the IDC.AggrMapping project.

    Summary:
    - Suppresses SonarAnalyzer warnings for specific members and namespaces.
    - Ensures intentional design choices are not flagged by static analysis tools.

    Remarks:
    > [!NOTE]
    These suppressions are applied at the assembly level and affect code analysis results.

    > [!TIP]
    Use this file to centralize and document all suppression attributes for maintainability.
*/



// Suppressions example for specified namespace
// Contoh penekanan untuk namespace dan turunannya
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage(
    category: "SonarAnalyzer",
    checkId: "S1192",
    Scope = "namespaceanddescendants",
    Target = "~N:IDC.AggrMapping",
    Justification = "Literal string digunakan secara sengaja untuk konfigurasi dan ekstensi."
)]

// Suppressions example for all members within a namespace
// Contoh penekanan untuk anggota tertentu
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage(
    category: "Sonar Code Smell",
    checkId: "S1135",
    Scope = "namespaceanddescendants",
    Target = "~N:IDC.AggrMapping",
    Justification = "TODO comments are used for development tracking and will be addressed before production releases."
)]

[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage(
    category: "SonarAnalyzer",
    checkId: "S2325",
    Scope = "namespaceanddescendants",
    Target = "~N:IDC.AggrMapping.Utilities.Models",
    Justification = "SonarQube salah analisa struktur kode, jadi gak bisa di paksa nurut."
)]

[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage(
    category: "SonarAnalyzer",
    checkId: "CA1822",
    Scope = "namespaceanddescendants",
    Target = "~N:IDC.AggrMapping.Utilities.Models",
    Justification = "CodeAnalysis salah analisa struktur kode, jadi gak bisa di paksa nurut."
)]

[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage(
    category: "SonarQube",
    checkId: "S2068",
    Scope = "member",
    Target = "~M:IDC.AggrMapping.Program.SetupPgsql(Microsoft.AspNetCore.Builder.WebApplicationBuilder)~Microsoft.AspNetCore.Builder.WebApplicationBuilder",
    Justification = "Sonarqube salah analisa, gak ada password di dalam method tersebut."
)]

[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage(
    category: "SonarQube",
    checkId: "S2068",
    Scope = "member",
    Target = "~M:IDC.AggrMapping.Program.SetupHangfire(Microsoft.AspNetCore.Builder.WebApplicationBuilder)~Microsoft.AspNetCore.Builder.WebApplicationBuilder",
    Justification = "Sonarqube salah analisa, gak ada password di dalam method tersebut."
)]
