using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;
using UnityEngine.Rendering;
using Verse;

namespace Puppeteer
{
	[HarmonyPatch(typeof(CameraDriver))]
	[HarmonyPatch(nameof(CameraDriver.CurrentViewRect), MethodType.Getter)]
	static class CameraDriver_CurrentViewRect_Patch
	{
		[HarmonyPriority(Priority.First)]
		public static bool Prefix(ref CellRect __result)
		{
			if (Renderer.fakeViewRect.IsEmpty) return true;
			__result = Renderer.fakeViewRect;
			return false;
		}
	}

	[HarmonyPatch(typeof(CameraDriver))]
	[HarmonyPatch(nameof(CameraDriver.CurrentZoom), MethodType.Getter)]
	static class CameraDriver_get_CurrentZoom_Patch
	{
		[HarmonyPriority(Priority.First)]
		public static bool Prefix(ref CameraZoomRange __result)
		{
			if (Renderer.fakeZoom == false) return true;
			__result = CameraZoomRange.Closest;
			return false;
		}
	}

	[HarmonyPatch(typeof(Pawn))]
	[HarmonyPatch(nameof(Pawn.Name), MethodType.Setter)]
	static class Pawn_set_Name_Patch
	{
		[HarmonyPriority(Priority.First)]
		public static void Postfix(Pawn __instance)
		{
			if (__instance.IsColonist)
				Controller.instance.SetEvent(PuppeteerEvent.ColonistsChanged);
		}
	}

	[HarmonyPatch]
	static class Area_Patches
	{
		public static IEnumerable<MethodBase> TargetMethods()
		{
			// RimWorld 1.5: Check if methods exist before yielding
			var method1 = AccessTools.Method(typeof(AreaManager), "NotifyEveryoneAreaRemoved");
			if (method1 != null) yield return method1;
			
			var method2 = AccessTools.Method(typeof(AreaManager), "TryMakeNewAllowed");
			if (method2 != null) yield return method2;
			
			// Area_Allowed.SetLabel may not exist in 1.5
			var areaAllowedType = AccessTools.TypeByName("RimWorld.Area_Allowed");
			if (areaAllowedType != null)
			{
				var method3 = AccessTools.Method(areaAllowedType, "SetLabel");
				if (method3 != null) yield return method3;
			}
		}

		[HarmonyPriority(Priority.First)]
		public static void Postfix()
		{
			Controller.instance.SetEvent(PuppeteerEvent.AreasChanged);
		}
	}

	[HarmonyPatch(typeof(Graphics))]
	[HarmonyPatch("Internal_DrawMesh")]
	static class Graphics_Internal_DrawMesh_Patch
	{
		[HarmonyPriority(Priority.First)]
		public static void Prefix(ref Matrix4x4 matrix)
		{
			if (Renderer.renderOffset == 0f) return;
			matrix = matrix.OffsetRef(new Vector3(Renderer.renderOffset, 0f, 0f));
		}
	}

	[HarmonyPatch(typeof(Graphics))]
	[HarmonyPatch("DrawMeshInstanced")]
	[HarmonyPatch(new[] { typeof(Mesh), typeof(int), typeof(Material), typeof(Matrix4x4[]), typeof(int), typeof(MaterialPropertyBlock), typeof(ShadowCastingMode), typeof(bool), typeof(int), typeof(Camera), typeof(LightProbeUsage), typeof(LightProbeProxyVolume) })]
	static class Graphics_DrawMeshInstanced_Patch
	{
		[HarmonyPriority(Priority.First)]
		public static void Prefix(Matrix4x4[] matrices)
		{
			if (Renderer.renderOffset == 0f) return;
			for (var i = 0; i < matrices.Length; i++)
				matrices[i] = matrices[i].Offset(Renderer.RenderOffsetVector);
		}
	}

	[HarmonyPatch(typeof(Graphics))]
	[HarmonyPatch("Internal_DrawMeshInstancedIndirect")]
	static class Graphics_Internal_DrawMeshInstancedIndirect_Patch
	{
		[HarmonyPriority(Priority.First)]
		public static void Prefix(ref Bounds bounds)
		{
			if (Renderer.RenderOffsetVector == Vector3.zero) return;
			bounds.center += Renderer.RenderOffsetVector;
		}
	}

	[HarmonyPatch(typeof(Graphics))]
	[HarmonyPatch("Internal_DrawMeshNow1")]
	static class Graphics_Internal_DrawMeshNow1_Patch
	{
		[HarmonyPriority(Priority.First)]
		public static void Prefix(ref Vector3 position)
		{
			if (Renderer.RenderOffsetVector == Vector3.zero) return;
			position += Renderer.RenderOffsetVector;
		}
	}

	[HarmonyPatch(typeof(Graphics))]
	[HarmonyPatch("Internal_DrawMeshNow2")]
	static class Graphics_Internal_DrawMeshNow2_Patch
	{
		[HarmonyPriority(Priority.First)]
		public static void Prefix(ref Matrix4x4 matrix)
		{
			if (Renderer.RenderOffsetVector == Vector3.zero) return;
			matrix = matrix.OffsetRef(Renderer.RenderOffsetVector);
		}
	}

	[HarmonyPatch(typeof(Game))]
	[HarmonyPatch(nameof(Game.UpdatePlay))]
	static class Game_UpdatePlay_Patch
	{
		[HarmonyPriority(Priority.First)]
		public static void Postfix()
		{
			if (Event.current.type == EventType.Repaint)
			{
				OperationQueue.Process(OperationType.Portrait);
				OperationQueue.Process(OperationType.RenderMap);
				Controller.instance.SetEvent(PuppeteerEvent.UpdateSocials);
				OperationQueue.Process(OperationType.SocialRelations);
			}
			Controller.instance.SetEvent(PuppeteerEvent.UpdateColonists);
		}
	}

	[HarmonyPatch(typeof(WindowStack))]
	[HarmonyPatch(nameof(WindowStack.WindowStackOnGUI))]
	static class WindowStack_WindowStackOnGUI_Patch
	{
		[HarmonyPriority(Priority.First)]
		public static void Postfix()
		{
			if (Event.current.type != EventType.Repaint) return;

			OperationQueue.Process(OperationType.Select);
			OperationQueue.Process(OperationType.Gear);
			OperationQueue.Process(OperationType.Inventory);
		}
	}

	[HarmonyPatch(typeof(Widgets))]
	[HarmonyPatch(nameof(Widgets.WidgetsOnGUI))]
	static class Widgets_WidgetsOnGUI_Patch
	{
		[HarmonyPriority(Priority.First)]
		public static void Postfix()
		{
			OperationQueue.Process(OperationType.Job);
			OperationQueue.Process(OperationType.SetState);
		}
	}

	[HarmonyPatch(typeof(PortraitsCache))]
	[HarmonyPatch(nameof(PortraitsCache.SetDirty))]
	static class PortraitsCache_SetDirty_Patch
	{
		[HarmonyPriority(Priority.First)]
		public static void Postfix(Pawn pawn)
		{
			Controller.instance.UpdatePortrait(pawn);
		}
	}

	// RimWorld 1.5: PawnRenderer tamamen değişti, artık PawnRenderTree kullanılıyor
	// Pawn.Tick üzerinden çalışıyoruz
	[HarmonyPatch(typeof(Pawn))]
	[HarmonyPatch(nameof(Pawn.Tick))]
	static class Pawn_Tick_Patch
	{
		[HarmonyPriority(Priority.First)]
		public static void Postfix(Pawn __instance)
		{
			if (State.pawnsToRefresh.Contains(__instance))
			{
				_ = State.pawnsToRefresh.Remove(__instance);
				// RimWorld 1.5: graphics artık farklı şekilde erişiliyor
				__instance.Drawer?.renderer?.SetAllGraphicsDirty();
				Controller.instance.UpdatePortrait(__instance);
				PortraitsCache.SetDirty(__instance);
			}
		}
	}

	// RimWorld 1.5: SetAnimatedPortraitsDirty için alternatif yaklaşım
	[HarmonyPatch(typeof(PortraitsCache))]
	[HarmonyPatch(nameof(PortraitsCache.Clear))]
	static class PortraitsCache_Clear_Patch
	{
		static readonly List<Pawn> previousChangedPawns = new List<Pawn>();

		[HarmonyPriority(Priority.First)]
		public static void Postfix()
		{
			// Tüm kolonistlerin portrait'lerini güncelle
			if (Find.CurrentMap != null)
			{
				foreach (var pawn in Find.CurrentMap.mapPawns.FreeColonists)
				{
					Controller.instance.UpdatePortrait(pawn);
				}
			}
		}
	}
}
