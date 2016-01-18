#region license

/*
EvrPlay - A simple media player which plays using the Enhanced Video Renderer
Copyright (C) 2008 andy vt
http://babvant.com

This license governs use of the accompanying software. If you use the software, you accept this license. If you do not accept the license, do not use the software.

1. Definitions
The terms "reproduce," "reproduction," "derivative works," and "distribution" have the same meaning here as under U.S. copyright law.
A "contribution" is the original software, or any additions or changes to the software.
A "contributor" is any person that distributes its contribution under this license.
"Licensed patents" are a contributor's patent claims that read directly on its contribution.

2. Grant of Rights
(A) Copyright Grant- Subject to the terms of this license, including the license conditions and limitations in section 3, each contributor grants you a non-exclusive, worldwide, royalty-free copyright license to reproduce its contribution, prepare derivative works of its contribution, and distribute its contribution or any derivative works that you create.
(B) Patent Grant- Subject to the terms of this license, including the license conditions and limitations in section 3, each contributor grants you a non-exclusive, worldwide, royalty-free license under its licensed patents to make, have made, use, sell, offer for sale, import, and/or otherwise dispose of its contribution in the software or derivative works of the contribution in the software.

3. Conditions and Limitations
(A) Reciprocal Grants- For any file you distribute that contains code from the software (in source code or binary format), you must provide recipients the source code to that file along with a copy of this license, which license will govern that file. Code that links to or derives from the software must be released under an OSI-certified open source license.
(B) No Trademark License- This license does not grant you rights to use any contributors' name, logo, or trademarks.
(C) If you bring a patent claim against any contributor over patents that you claim are infringed by the software, your patent license from such contributor to the software ends automatically.
(D) If you distribute any portion of the software, you must retain all copyright, patent, trademark, and attribution notices that are present in the software.
(E) If you distribute any portion of the software in source code form, you may do so only under this license by including a complete copy of this license with your distribution. If you distribute any portion of the software in compiled or object code form, you may only do so under a license that complies with this license.
(F) The software is licensed "as-is." You bear the risk of using it. The contributors give no express warranties, guarantees or conditions. You may have additional consumer rights under your local laws which this license cannot change. To the extent permitted under your local laws, the contributors exclude the implied warranties of merchantability, fitness for a particular purpose and non-infringement.

*/

#endregion

using System.Reflection;
using System.Runtime.CompilerServices;

//
// Les informations g�n�rales relatives � un assembly d�pendent de 
// l'ensemble d'attributs suivant. Pour modifier les informations
// associ�es � un assembly, changez les valeurs de ces attributs.
//
[assembly: AssemblyTitle("EVRPlay")]
[assembly: AssemblyDescription("A simple media player which plays using the Enhanced Video Renderer")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("babgvant.com")]
[assembly: AssemblyProduct("")]
[assembly: AssemblyCopyright("Copyright (C) 2008 andy vt")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]		

//
// Les informations de version pour un assembly se composent des quatre valeurs suivantes�:
//
//      Version principale
//      Version secondaire 
//      Num�ro de build
//      R�vision
//
// Vous pouvez sp�cifier toutes les valeurs ou indiquer des num�ros de r�vision et de build par d�faut 
// en utilisant '*', comme ci-dessous :

[assembly: AssemblyVersion("1.0.0.8")]

//
// Pour signer votre assembly, vous devez sp�cifier la cl� � utiliser. Consultez 
// la documentation Microsoft .NET Framework pour plus d'informations sur la signature d'un assembly.
//
// Utilisez les attributs ci-dessous pour contr�ler la cl� utilis�e lors de la signature. 
//
// Remarques�: 
//   (*) Si aucune cl� n'est sp�cifi�e, l'assembly n'est pas sign�.
//   (*) KeyName fait r�f�rence � une cl� install�e dans le fournisseur de
//       services cryptographiques (CSP) de votre ordinateur. KeyFile fait r�f�rence � un fichier qui contient
//       une cl�.
//   (*) Si les valeurs de KeyFile et de KeyName sont sp�cifi�es, le 
//       traitement suivant se produit�:
//       (1) Si KeyName se trouve dans le CSP, la cl� est utilis�e.
//       (2) Si KeyName n'existe pas mais que KeyFile existe, la cl� 
//           de KeyFile est install�e dans le CSP et utilis�e.
//   (*) Pour cr�er KeyFile, vous pouvez utiliser l'utilitaire sn.exe (Strong Name, Nom fort).
//        Lors de la sp�cification de KeyFile, son emplacement doit �tre
//        relatif au r�pertoire de sortie du projet qui est
//       %Project Directory%\obj\<configuration>. Par exemple, si votre KeyFile se trouve
//       dans le r�pertoire du projet, vous devez sp�cifier l'attribut 
//       AssemblyKeyFile sous la forme [assembly: AssemblyKeyFile("..\\..\\mykey.snk")]
//   (*) DelaySign (signature diff�r�e) est une option avanc�e. Pour plus d'informations, consultez la
//       documentation Microsoft .NET Framework.
//
[assembly: AssemblyDelaySign(false)]
[assembly: AssemblyKeyFile("")]
[assembly: AssemblyKeyName("")]
