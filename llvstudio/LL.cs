using System;
using System.Runtime.InteropServices;

using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System.Text;
using System.IO;
using System.CodeDom.Compiler;

namespace LL
{
	[PackageRegistration(UseManagedResourcesOnly = true)]
	[InstalledProductRegistration("LL", "LL Parser generator", "1.0")]
	[Guid("DD0325A1-0019-48C6-9324-55BE887D707C")]
	[ComVisible(true)]
	[ProvideObject(typeof(LL))]
	[CodeGeneratorRegistration(typeof(LL), "LL", "{FAE04EC1-301F-11D3-BF4B-00C04F79EFBC}", GeneratesDesignTimeSource = true)]
	public sealed class LL : IVsSingleFileGenerator
	{

		#region IVsSingleFileGenerator Members

		public int DefaultExtension(out string pbstrDefaultExtension)
		{
			pbstrDefaultExtension = ".cs";
			return pbstrDefaultExtension.Length;
		}

		public int Generate(string wszInputFilePath, string bstrInputFileContents,
			string wszDefaultNamespace, IntPtr[] rgbOutputFileContents,
			out uint pcbOutput, IVsGeneratorProgress pGenerateProgress)
		{
			pcbOutput = 0;
			try
			{
				ThreadHelper.ThrowIfNotOnUIThread();
				pGenerateProgress.Progress(0, 4);
				var hasErrors = false;
				using (var stm = new MemoryStream())
				{
					
					EbnfDocument ebnf = null;
					try
					{
						ebnf = EbnfDocument.ReadFrom(wszInputFilePath);
					}
					catch(ExpectingException ee)
					{
						hasErrors = true;
						ThreadHelper.ThrowIfNotOnUIThread();
						pGenerateProgress.GeneratorError(0, 0, "Error parsing the EBNF: " + ee.Message, (uint)ee.Line-1, (uint)ee.Column-1);
					}
					ThreadHelper.ThrowIfNotOnUIThread();
					pGenerateProgress.Progress(1, 4);
					foreach (var msg in ebnf.Validate(false))
					{
						switch(msg.ErrorLevel)
						{
							case EbnfErrorLevel.Error:
								ThreadHelper.ThrowIfNotOnUIThread();
								pGenerateProgress.GeneratorError(0, 0, "EBNF "+msg.Message, (uint)msg.Line-1, (uint)msg.Column-1);
								hasErrors = true;
								break;
							case EbnfErrorLevel.Warning:
								ThreadHelper.ThrowIfNotOnUIThread();
								pGenerateProgress.GeneratorError(1, 0, "EBNF " + msg.Message, (uint)msg.Line-1, (uint)msg.Column-1);
								break;
						}
						
					}
					ThreadHelper.ThrowIfNotOnUIThread();
					pGenerateProgress.Progress(3, 4);
					var cfg = ebnf.ToCfg();
					foreach (var msg in cfg.PrepareLL1(false))
					{
						switch (msg.ErrorLevel)
						{
							case CfgErrorLevel.Error:
								ThreadHelper.ThrowIfNotOnUIThread();
								pGenerateProgress.GeneratorError(0, 0, "CFG " + msg.Message, 0, 0);
								hasErrors = true;
								break;
							case CfgErrorLevel.Warning:
								ThreadHelper.ThrowIfNotOnUIThread();
								pGenerateProgress.GeneratorError(1, 0, "CFG " + msg.Message, 0, 0);
								break;
						}
					}
					if (!hasErrors)
					{
						var sw = new StreamWriter(stm);
						LLCodeGenerator.WriteParserAndGeneratorClassesTo(ebnf, cfg, wszDefaultNamespace,null, Path.GetFileNameWithoutExtension(wszInputFilePath), "cs", sw);
						sw.Flush();
						int length = (int)stm.Length;
						rgbOutputFileContents[0] = Marshal.AllocCoTaskMem(length);
						Marshal.Copy(stm.GetBuffer(), 0, rgbOutputFileContents[0], length);
						pcbOutput = (uint)length;
						
					}
					ThreadHelper.ThrowIfNotOnUIThread();
					pGenerateProgress.Progress(4, 4);


				}

			}
			catch (Exception ex)
			{
				string s = string.Concat("/* ", ex.Message, " */");
				byte[] b = Encoding.UTF8.GetBytes(s);
				int length = b.Length;
				rgbOutputFileContents[0] = Marshal.AllocCoTaskMem(length);
				Marshal.Copy(b, 0, rgbOutputFileContents[0], length);
				pcbOutput = (uint)length;
			}
			return VSConstants.S_OK;
		}

		#endregion
	}
}

