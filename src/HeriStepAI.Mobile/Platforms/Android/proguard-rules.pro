# Keep all WebView related classes
-keep class android.webkit.** { *; }
-keepclassmembers class android.webkit.** { *; }

# Keep WebChromeClient and WebViewClient
-keep class * extends android.webkit.WebChromeClient { *; }
-keep class * extends android.webkit.WebViewClient { *; }

# Keep JavaScript interface classes
-keepclassmembers class * {
    @android.webkit.JavascriptInterface <methods>;
}

# Keep classes that interact with JavaScript
-keepattributes JavascriptInterface
-keepattributes *Annotation*

# Keep MapPage and its inner classes
-keep class HeriStepAI.Mobile.Views.MapPage { *; }
-keep class HeriStepAI.Mobile.Views.MapPage$** { *; }

# Keep all native methods (for JavaScript callbacks)
-keepclasseswithmembernames class * {
    native <methods>;
}

# Preserve line numbers for debugging
-keepattributes SourceFile,LineNumberTable
-renamesourcefileattribute SourceFile

# Don't warn about missing classes that might not be used
-dontwarn android.webkit.**

# Keep MAUI WebView handler classes
-keep class Microsoft.Maui.** { *; }
-keep class Microsoft.Maui.Handlers.** { *; }

# Disable obfuscation for debugging
-dontobfuscate
