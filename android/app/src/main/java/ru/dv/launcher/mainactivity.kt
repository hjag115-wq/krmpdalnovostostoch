package ru.dv.launcher

import android.app.Activity
import android.content.Intent
import android.graphics.Color
import android.net.Uri
import android.os.Bundle
import android.view.Gravity
import android.widget.*
import java.net.*

// ===== НАСТРОЙКИ ПРОЕКТА =====
object Config {
    const val HOST = "31.77.232.41"
    const val PORT = 3431
    const val GAME_PACKAGE = "com.CHANGE.ME"          // package name клиента (также поправьте в AndroidManifest.xml)
    const val DOWNLOAD_URL = "https://example.com/"   // откуда скачать клиент
    const val VK_URL = "https://vk.com/"
}
// =============================

class MainActivity : Activity() {
    private lateinit var status: TextView

    override fun onCreate(s: Bundle?) {
        super.onCreate(s)
        val prefs = getSharedPreferences("dv", MODE_PRIVATE)
        val root = LinearLayout(this).apply {
            orientation = LinearLayout.VERTICAL; gravity = Gravity.CENTER
            setBackgroundColor(Color.rgb(18, 20, 28)); setPadding(64, 64, 64, 64)
        }
        fun tv(t: String, size: Float, c: Int) = TextView(this).apply {
            text = t; textSize = size; setTextColor(c); gravity = Gravity.CENTER
        }
        root.addView(tv("ДАЛЬНЕВОСТОЧНЫЙ", 28f, Color.rgb(214, 40, 57)))
        root.addView(tv("КРМП · ролевой проект", 14f, Color.GRAY))
        val nick = EditText(this).apply {
            hint = "Никнейм"; setText(prefs.getString("nick", "")); setHintTextColor(Color.GRAY)
            setTextColor(Color.WHITE); gravity = Gravity.CENTER
        }
        root.addView(nick, LinearLayout.LayoutParams(-1, -2).apply { topMargin = 96 })
        status = tv("Сервер: опрос…", 14f, Color.LTGRAY)
        root.addView(status, LinearLayout.LayoutParams(-1, -2).apply { topMargin = 32 })
        root.addView(tv("${Config.HOST}:${Config.PORT}", 12f, Color.GRAY))

        fun btn(t: String, c: Int, f: () -> Unit) = Button(this).apply {
            text = t; setTextColor(Color.WHITE); setBackgroundColor(c); setOnClickListener { f() }
        }
        root.addView(btn("ИГРАТЬ", Color.rgb(214, 40, 57)) {
            val n = nick.text.toString().trim()
            if (!Regex("^[A-Za-z0-9_\\[\\]\\.$=@]{3,20}$").matches(n)) {
                Toast.makeText(this, "Ник: 3–20 символов, латиница/цифры/_", Toast.LENGTH_LONG).show()
            } else {
                prefs.edit().putString("nick", n).apply()
                val i = packageManager.getLaunchIntentForPackage(Config.GAME_PACKAGE)
                if (i == null) startActivity(Intent(Intent.ACTION_VIEW, Uri.parse(Config.DOWNLOAD_URL)))
                else startActivity(i.putExtra("nickname", n).putExtra("ip", Config.HOST).putExtra("port", Config.PORT))
            }
        }, LinearLayout.LayoutParams(-1, 150).apply { topMargin = 64 })
        root.addView(btn("ВКонтакте", Color.rgb(45, 49, 66)) {
            startActivity(Intent(Intent.ACTION_VIEW, Uri.parse(Config.VK_URL)))
        }, LinearLayout.LayoutParams(-1, 130).apply { topMargin = 24 })
        setContentView(root)
        refresh()
    }

    private fun refresh() = Thread {
        val msg = try {
            val ip = InetAddress.getByName(Config.HOST).address
            val req = "SAMP".toByteArray() + ip + byteArrayOf((Config.PORT and 255).toByte(), (Config.PORT shr 8).toByte(), 'i'.code.toByte())
            DatagramSocket().use { s ->
                s.soTimeout = 2000
                val t = System.currentTimeMillis()
                s.send(DatagramPacket(req, req.size, InetAddress.getByName(Config.HOST), Config.PORT))
                val b = ByteArray(512); s.receive(DatagramPacket(b, b.size))
                val ping = System.currentTimeMillis() - t
                val p = (b[12].toInt() and 255) or ((b[13].toInt() and 255) shl 8)
                val m = (b[14].toInt() and 255) or ((b[15].toInt() and 255) shl 8)
                "Онлайн: $p/$m · Пинг: $ping мс"
            }
        } catch (e: Exception) { "Сервер не отвечает" }
        runOnUiThread { status.text = msg }
    }.start()
}
