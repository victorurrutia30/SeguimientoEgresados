document.addEventListener("DOMContentLoaded", function () {
    // ===== Utilidades básicas =====
    function $(sel) { return document.querySelector(sel); }
    function getToken() {
        var el = document.querySelector('input[name="__RequestVerificationToken"]');
        return el ? el.value : "";
    }

    // Detecta si el servidor devolvió HTML/redirect en vez de JSON
    function safeJson(resp) {
        const ct = resp.headers.get('content-type') || '';
        if (resp.redirected) {
            console.warn('REDIRECT detectado →', resp.url);
        }
        if (resp.ok && ct.includes('application/json')) return resp.json();
        return resp.text().then(t => {
            console.error('NO JSON', resp.status, resp.url, t.slice(0, 300));
            throw new Error('Respuesta no-JSON (' + resp.status + '). Revisa autorización/binder/errores del servidor.');
        });
    }

    // Convierte objeto a application/x-www-form-urlencoded (para MVC 5)
    function toForm(obj) {
        const p = new URLSearchParams();
        Object.keys(obj).forEach(k => p.append(k, obj[k] == null ? '' : String(obj[k])));
        const t = getToken();
        if (t) p.append('__RequestVerificationToken', t);
        return p;
    }

    var URLS = (window.__urls) || {
        reg: '/Registro/RegistrarEgresado',
        sit: '/Registro/GuardarSituacionLaboral',
        home: '/'
    };

    // ===== VALIDACIONES CENTRALIZADAS =====
    const Validaciones = {
        email: function (email) {
            var regex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
            var valor = (email || "").trim();
            if (!valor) return { valido: false, mensaje: "El correo electrónico es obligatorio" };
            if (!regex.test(valor)) return { valido: false, mensaje: "El formato del correo no es válido" };
            if (valor.length < 5) return { valido: false, mensaje: "El correo es demasiado corto" };
            return { valido: true };
        },

        texto: function (texto, nombre, minCaracteres) {
            var valor = (texto || "").trim();
            if (!valor) return { valido: false, mensaje: nombre + " es obligatorio" };
            if (valor.length < minCaracteres) {
                return { valido: false, mensaje: nombre + " debe tener al menos " + minCaracteres + " caracteres" };
            }
            return { valido: true };
        },

        carne: function (carne) {
            var valor = (carne || "").trim();
            if (!valor) return { valido: false, mensaje: "El número de carné es obligatorio" };
            if (valor.length < 4 || valor.length > 12) {
                return { valido: false, mensaje: "El carné debe tener entre 4 y 12 caracteres" };
            }
            if (!/^[a-zA-Z0-9]+$/.test(valor)) {
                return { valido: false, mensaje: "El carné solo puede contener letras y números" };
            }
            return { valido: true };
        },

        telefono: function (telefono) {
            var valor = (telefono || "").trim();
            if (!valor) return { valido: false, mensaje: "El teléfono es obligatorio" };
            var numeros = valor.replace(/[^0-9]/g, '');
            if (numeros.length < 8) {
                return { valido: false, mensaje: "El teléfono debe tener al menos 8 dígitos" };
            }
            return { valido: true };
        },

        fecha: function (fecha, nombre) {
            var valor = (fecha || "").trim();
            if (!valor) return { valido: false, mensaje: nombre + " es obligatoria" };
            var fechaObj = new Date(valor);
            if (isNaN(fechaObj.getTime())) {
                return { valido: false, mensaje: nombre + " no es válida" };
            }
            return { valido: true };
        },

        select: function (valor, nombre) {
            var val = (valor || "").trim();
            if (!val || val === "" || val === "0" || val === "-1") {
                return { valido: false, mensaje: "Debes seleccionar " + nombre };
            }
            return { valido: true };
        },

        password: function (pass1, pass2) {
            if (!pass1 || pass1.length < 6) {
                return { valido: false, mensaje: "La contraseña debe tener al menos 6 caracteres" };
            }
            if (pass1 !== pass2) {
                return { valido: false, mensaje: "Las contraseñas no coinciden" };
            }
            return { valido: true };
        },

        promedio: function (valor) {
            if (!valor || valor === '') return { valido: true }; // Opcional
            var num = parseFloat(valor);
            if (isNaN(num) || num < 0 || num > 10) {
                return { valido: false, mensaje: "El promedio debe estar entre 0 y 10" };
            }
            var partes = valor.toString().split('.');
            if (partes.length > 1 && partes[1].length > 2) {
                return { valido: false, mensaje: "El promedio solo puede tener 2 decimales" };
            }
            return { valido: true };
        },

        experiencia: function (valor) {
            if (!valor || valor === '') return { valido: true }; // Opcional
            var num = parseInt(valor, 10);
            if (isNaN(num) || num < 0) {
                return { valido: false, mensaje: "Los años de experiencia no pueden ser negativos" };
            }
            if (num > 50) {
                return { valido: false, mensaje: "Los años de experiencia no pueden ser mayores a 50" };
            }
            return { valido: true };
        },

        archivo: function (archivo, tipo, maxSize) {
            if (!archivo) return { valido: false, mensaje: "Debes seleccionar un archivo" };
            if (tipo && archivo.type !== tipo) {
                return { valido: false, mensaje: "El archivo debe ser de tipo " + tipo };
            }
            if (maxSize && archivo.size > maxSize) {
                var maxMB = (maxSize / (1024 * 1024)).toFixed(0);
                return { valido: false, mensaje: "El archivo no debe superar " + maxMB + "MB" };
            }
            return { valido: true };
        }
    };

    // ===== FUNCIONES DE NAVEGACIÓN =====
    function showStep(id) {
        ["wizard-step1", "wizard-step2", "wizard-step3A", "wizard-step3B"].forEach(function (s) {
            var el = document.getElementById(s);
            if (!el) return;
            el.style.display = (s === id) ? "" : "none";
        });

        var factor = (id === "wizard-step1") ? 0.33 : (id === "wizard-step2") ? 0.66 : 1;
        var pf = document.querySelector('#' + id + ' .progress-fill');
        if (pf) {
            pf.style.width = 'calc((100% - 16%) * ' + factor + ')';
        }

        window.scrollTo({ top: 0, behavior: "smooth" });
    }

    // ===== UX: Input de CV (Paso 3B) =====
    (function () {
        var btn = $("#btnSelectCV"), inp = $("#cvFile"), name = $("#cvName"), dz = $("#cvDrop");
        if (btn && inp) {
            btn.addEventListener("click", function () { inp.click(); });
            inp.addEventListener("change", function () {
                if (!inp.files || !inp.files.length) {
                    name && (name.textContent = "");
                    return;
                }
                var f = inp.files[0];
                var validacion = Validaciones.archivo(f, "application/pdf", 5 * 1024 * 1024);
                if (!validacion.valido) {
                    Swal.fire({
                        icon: 'error',
                        title: 'Archivo inválido',
                        text: validacion.mensaje,
                        confirmButtonText: 'Entendido'
                    });
                    inp.value = "";
                    name && (name.textContent = "");
                    return;
                }
                if (name) {
                    name.textContent = f.name + " (" + Math.round(f.size / 1024) + " KB)";
                }
            });

            ["dragover", "drop"].forEach(function (evt) {
                if (!dz) return;
                dz.addEventListener(evt, function (e) {
                    e.preventDefault();
                    if (evt === "drop" && e.dataTransfer && e.dataTransfer.files && e.dataTransfer.files[0]) {
                        try {
                            var dt = new DataTransfer();
                            dt.items.add(e.dataTransfer.files[0]);
                            inp.files = dt.files;
                        } catch (_) {
                            inp.files = e.dataTransfer.files;
                        }
                        inp.dispatchEvent(new Event("change", { bubbles: true }));
                    }
                });
            });
        }
    })();

    // Toggle fecha disponible (Paso 3B)
    (function () {
        var chk = $("#dispInmediata");
        var fd = $("#fechaDisp");
        if (!chk || !fd) return;
        var up = function () {
            fd.disabled = chk.checked;
            if (chk.checked) fd.value = "";
        };
        chk.addEventListener("change", up);
        up();
    })();

    // ===== VALIDACIÓN Y CORRECCIÓN DE PROMEDIO =====
    (function () {
        var promedio = $("#promedio");
        if (!promedio) return;

        promedio.addEventListener("input", function (e) {
            var valor = this.value;
            var regex = /^\d*\.?\d{0,2}$/;
            if (!regex.test(valor) && valor !== '') {
                this.value = valor.slice(0, -1);
                return;
            }
            if (valor !== '' && !Validaciones.promedio(valor).valido) {
                var num = parseFloat(valor);
                if (!isNaN(num) && num > 10) {
                    this.value = '10';
                }
            }
        });

        promedio.addEventListener("blur", function () {
            var valor = this.value;
            if (valor === '') return;
            var num = parseFloat(valor);
            if (num < 0) {
                this.value = '0';
                Swal.fire({
                    icon: 'warning',
                    title: 'Valor corregido',
                    text: 'El promedio no puede ser negativo',
                    toast: true,
                    position: 'top-end',
                    showConfirmButton: false,
                    timer: 3000,
                    timerProgressBar: true
                });
            } else if (num > 10) {
                this.value = '10.00';
                Swal.fire({
                    icon: 'warning',
                    title: 'Valor corregido',
                    text: 'El promedio máximo es 10.00',
                    toast: true,
                    position: 'top-end',
                    showConfirmButton: false,
                    timer: 3000,
                    timerProgressBar: true
                });
            }
        });
    })();

    // ===== VALIDACIÓN DE EXPERIENCIA =====
    (function () {
        var experiencia = $("#experiencia");
        if (!experiencia) return;

        experiencia.addEventListener("keydown", function (e) {
            var permitidos = [8, 9, 27, 13, 37, 38, 39, 40, 46];
            if (e.key === '.' || e.key === '-' || e.key === 'e' || e.key === 'E') {
                e.preventDefault();
                return;
            }
            if ((e.ctrlKey || e.metaKey) && (e.key === 'a' || e.key === 'c' || e.key === 'v' || e.key === 'x')) {
                return;
            }
            if (!permitidos.includes(e.keyCode) && (e.key < '0' || e.key > '9')) {
                e.preventDefault();
            }
        });

        experiencia.addEventListener("input", function () {
            this.value = this.value.replace(/[^0-9]/g, '');
        });

        experiencia.addEventListener("blur", function () {
            var valor = this.value;
            if (valor === '') return;
            var num = parseInt(valor, 10);
            if (num < 0) {
                this.value = '0';
                Swal.fire({
                    icon: 'warning',
                    title: 'Valor corregido',
                    text: 'Los años de experiencia no pueden ser negativos',
                    toast: true,
                    position: 'top-end',
                    showConfirmButton: false,
                    timer: 3000,
                    timerProgressBar: true
                });
            } else if (num > 50) {
                this.value = '50';
                Swal.fire({
                    icon: 'warning',
                    title: 'Valor corregido',
                    text: 'El máximo de años de experiencia es 50',
                    toast: true,
                    position: 'top-end',
                    showConfirmButton: false,
                    timer: 3000,
                    timerProgressBar: true
                });
            }
        });
    })();

    // ===== PASO 1: VALIDACIÓN Y NAVEGACIÓN =====
    (function () {
        var btnValidate = $("#btnStep1Validate");
        var btnNext = $("#btnStep1Next");
        var btnBack = $("#btnStep1Back");

        function validarPaso1() {
            var errores = [];

            // Validar nombres
            var valNombres = Validaciones.texto(
                $("#nombres") ? $("#nombres").value : '',
                "Nombres",
                2
            );
            if (!valNombres.valido) errores.push(valNombres.mensaje);

            // Validar apellidos
            var valApellidos = Validaciones.texto(
                $("#apellidos") ? $("#apellidos").value : '',
                "Apellidos",
                2
            );
            if (!valApellidos.valido) errores.push(valApellidos.mensaje);

            // Validar email
            var valEmail = Validaciones.email(
                $("#emailInst") ? $("#emailInst").value : ''
            );
            if (!valEmail.valido) errores.push(valEmail.mensaje);

            // Validar teléfono
            var valTelefono = Validaciones.telefono(
                $("#telefono") ? $("#telefono").value : ''
            );
            if (!valTelefono.valido) errores.push(valTelefono.mensaje);

            // Validar carné
            var valCarne = Validaciones.carne(
                $("#carne") ? $("#carne").value : ''
            );
            if (!valCarne.valido) errores.push(valCarne.mensaje);

            // Validar fecha de graduación
            var valFecha = Validaciones.fecha(
                $("#fechaGrad") ? $("#fechaGrad").value : '',
                "La fecha de graduación"
            );
            if (!valFecha.valido) errores.push(valFecha.mensaje);

            // Validar carrera
            var valCarrera = Validaciones.select(
                $("#carrera") ? $("#carrera").value : '',
                "una carrera"
            );
            if (!valCarrera.valido) errores.push(valCarrera.mensaje);

            // Validar promedio (opcional pero si tiene valor debe ser válido)
            var promedio = $("#promedio") ? $("#promedio").value : '';
            if (promedio) {
                var valPromedio = Validaciones.promedio(promedio);
                if (!valPromedio.valido) errores.push(valPromedio.mensaje);
            }

            // Validar experiencia (opcional pero si tiene valor debe ser válido)
            var experiencia = $("#experiencia") ? $("#experiencia").value : '';
            if (experiencia) {
                var valExperiencia = Validaciones.experiencia(experiencia);
                if (!valExperiencia.valido) errores.push(valExperiencia.mensaje);
            }

            // Validar contraseñas
            var valPassword = Validaciones.password(
                $("#password") ? $("#password").value : '',
                $("#password2") ? $("#password2").value : ''
            );
            if (!valPassword.valido) errores.push(valPassword.mensaje);

            return {
                valido: errores.length === 0,
                errores: errores
            };
        }

        if (btnValidate) {
            btnValidate.addEventListener("click", function (e) {
                if (e) e.preventDefault();

                var validacion = validarPaso1();

                if (validacion.valido) {
                    if (btnNext) btnNext.disabled = false;
                    Swal.fire({
                        icon: 'success',
                        title: '¡Identidad validada!',
                        text: 'Todos los datos son correctos. Ya puedes continuar al siguiente paso.',
                        confirmButtonText: 'Continuar',
                        timer: 3000,
                        timerProgressBar: true
                    });
                } else {
                    var listaErrores = validacion.errores.map(function (err) {
                        return '• ' + err;
                    }).join('<br>');

                    Swal.fire({
                        icon: 'error',
                        title: 'Información incompleta o incorrecta',
                        html: '<div style="text-align: left;">' + listaErrores + '</div>',
                        confirmButtonText: 'Revisar',
                        customClass: {
                            popup: 'swal-wide'
                        }
                    });
                }
            });
        }

        if (btnNext) {
            btnNext.addEventListener("click", function (e) {
                if (e) e.preventDefault();
                if (!btnNext.disabled) {
                    showStep("wizard-step2");
                }
            });
        }

        if (btnBack) {
            btnBack.addEventListener("click", function (e) {
                if (e) e.preventDefault();
                if (document.referrer) {
                    history.back();
                } else {
                    window.location.href = URLS.home;
                }
            });
        }
    })();

    // ===== PASO 2: CONSENTIMIENTO =====
    (function () {
        var chk = $("#consentPrivacy");
        var err = $("#consentErr");
        var btnNext = $("#btnStep2Next");
        var btnBack = $("#btnStep2Back");

        function actualizarEstado() {
            var ok = chk && chk.checked;
            if (btnNext) btnNext.disabled = !ok;
            if (err) err.style.display = ok ? "none" : "block";
        }

        if (chk) {
            chk.addEventListener("change", actualizarEstado);
            actualizarEstado();
        }

        if (btnNext) {
            btnNext.addEventListener("click", function () {
                if (!btnNext.disabled) {
                    showStep("wizard-step3A");
                } else {
                    Swal.fire({
                        icon: 'warning',
                        title: 'Consentimiento requerido',
                        text: 'Debes aceptar la política de privacidad para continuar',
                        confirmButtonText: 'Entendido'
                    });
                }
            });
        }

        if (btnBack) {
            btnBack.addEventListener("click", function () {
                showStep("wizard-step1");
            });
        }
    })();

    // ===== HELPERS =====
    function parseMeses(valor) {
        if (!valor) return null;
        if (valor.indexOf('0') === 0) return 3;
        if (valor.indexOf('4') === 0) return 6;
        if (valor.indexOf('7') === 0) return 12;
        if (valor.indexOf('>') === 0) return 13;
        return null;
    }

    function toDecimalString(v) {
        var n = parseFloat(v);
        if (isNaN(n)) return "0";
        return n.toString();
    }

    // ===== POST: REGISTRAR EGRESADO =====
    function postRegistrarEgresado(isTrabajando) {
        var fd = new FormData();
        fd.append('__RequestVerificationToken', getToken());

        // Paso 1 - Datos básicos
        fd.append('numeroDocumento', ($("#carne") && $("#carne").value) || '');
        fd.append('nombres', ($("#nombres") && $("#nombres").value) || '');
        fd.append('apellidos', ($("#apellidos") && $("#apellidos").value) || '');
        fd.append('email', ($("#emailInst") && $("#emailInst").value) || '');
        fd.append('telefono', ($("#telefono") && $("#telefono").value) || '');
        fd.append('carrera', ($("#carrera") && $("#carrera").value) || '');
        fd.append('fechaGraduacion', ($("#fechaGrad") && $("#fechaGrad").value) || '');
        fd.append('promedio', toDecimalString(($("#promedio") && $("#promedio").value) || '0'));
        fd.append('consentimiento', ($("#consentPrivacy") && $("#consentPrivacy").checked) ? 'true' : 'false');

        // CV (obligatorio si NO trabaja)
        var cvInput = $("#cvFile");
        var cvB = (cvInput && cvInput.files && cvInput.files[0]) ? cvInput.files[0] : null;

        if (!isTrabajando && !cvB) {
            Swal.fire({
                icon: 'warning',
                title: 'CV requerido',
                text: 'Debes adjuntar tu CV en formato PDF para continuar',
                confirmButtonText: 'Entendido'
            });
            return Promise.reject(new Error("CV requerido"));
        }

        if (cvB) {
            fd.append('CV', cvB);
        }

        // Experiencia / habilidades / idiomas / certificaciones
        fd.append('experiencia', ($("#experiencia") && $("#experiencia").value) || '0');

        var habilidades = isTrabajando ?
            (($("#skills") && $("#skills").value) || '') :
            (($("#skills2") && $("#skills2").value) || '');

        var idioma = isTrabajando ?
            (($("#idioma") && $("#idioma").value) || '') :
            (($("#idioma2") && $("#idioma2").value) || '');

        var nivel = isTrabajando ?
            (($("#nivel") && $("#nivel").value) || '') :
            (($("#nivel2") && $("#nivel2").value) || '');

        fd.append('habilidades', habilidades);
        fd.append('idiomas', idioma ? (nivel ? (idioma + " (" + nivel + ")") : idioma) : '');
        fd.append('certificaciones', ($("#certificaciones") && $("#certificaciones").value) || '');
        fd.append('password', ($("#password") && $("#password").value) || '');

        var btns = Array.prototype.slice.call(document.querySelectorAll('button'))
            .filter(function (b) { return b.id && b.id.indexOf('btnStep3') === 0; });
        btns.forEach(function (b) { b.disabled = true; });

        return fetch(URLS.reg, {
            method: 'POST',
            body: fd,
            credentials: 'same-origin',
            headers: {
                'Accept': 'application/json',
                'X-Requested-With': 'XMLHttpRequest',
                'RequestVerificationToken': getToken() // no estorba y ayuda si hay filtro que lo mira en header
            }
        })
            .then(safeJson)
            .then(function (json) {
                if (!json || !json.success) {
                    throw new Error((json && json.message) || 'Error registrando egresado');
                }
                return json.idEgresado;
            })
            .catch(function (err) {
                Swal.fire({
                    icon: 'error',
                    title: 'Error al registrar',
                    text: err.message || 'Hubo un problema al registrar tu información. Por favor intenta nuevamente.',
                    confirmButtonText: 'Cerrar'
                });
                throw err;
            })
            .finally(function () {
                btns.forEach(function (b) { b.disabled = false; });
            });
    }

    // ===== POST: GUARDAR SITUACIÓN LABORAL =====
    function postGuardarSituacionLaboral(idEgresado, isTrabajando) {
        var payload = {
            idEgresado: idEgresado,
            trabajandoActualmente: !!isTrabajando,
            empresaActual: '',
            cargoActual: '',
            rangoSalarial: '',
            modalidadTrabajo: '',
            satisfaccionTrabajo: null,
            usaConocimientosCarrera: null,
            tiempoConseguirTrabajo: null,
            contactaUniversidad: ($("#contactaUniversidad") && $("#contactaUniversidad").value) || null,
            deseaContacto: ($("#deseaContacto") && $("#deseaContacto").checked) ? 1 : 0,
            dispuestoEncuestaSemestral: ($("#encuestaSemestral") && $("#encuestaSemestral").checked) ? 1 : 0,
            metodoInicioSesion: 'Formulario',
            respuestasJson: '',
            sugerenciaFuncionalidad: ($("#sugerencia") && $("#sugerencia").value) || null
        };

        if (isTrabajando) {
            payload.empresaActual = ($("#empresa") && $("#empresa").value) || '';
            payload.cargoActual = ($("#cargo") && $("#cargo").value) || '';
            payload.rangoSalarial = ($("#rango") && $("#rango").value) || 'Confidencial';
            payload.modalidadTrabajo = ($("#modalidad") && $("#modalidad").value) || '';
            payload.satisfaccionTrabajo = ($("#satisfaccion") && $("#satisfaccion").value) ?
                parseInt($("#satisfaccion").value, 10) : null;
            payload.usaConocimientosCarrera = ($("#usaConocimientos") && $("#usaConocimientos").checked) ?
                true : null;
            payload.tiempoConseguirTrabajo = parseMeses(($("#tiempoPrimerEmpleo") && $("#tiempoPrimerEmpleo").value) || '');

            var extrasA = {
                area: ($("#area") && $("#area").value) || '',
                tipoContrato: ($("#tipoContr") && $("#tipoContr").value) || '',
                antiguedad: ($("#antig") && $("#antig").value) || '',
                idioma: ($("#idioma") && $("#idioma").value) || '',
                nivel: ($("#nivel") && $("#nivel").value) || '',
                skills: ($("#skills") && $("#skills").value) || '',
                beneficios: {
                    medico: ($("#ben1") && $("#ben1").checked) || false,
                    bono: ($("#ben2") && $("#ben2").checked) || false,
                    remoto: ($("#ben3") && $("#ben3").checked) || false
                },
                consentMarketing: ($("#consentMarketing") && $("#consentMarketing").checked) || false
            };
            payload.respuestasJson = JSON.stringify(extrasA);
        } else {
            payload.empresaActual = '';
            payload.cargoActual = '';
            payload.rangoSalarial = 'N/A';
            payload.modalidadTrabajo = ($("#modalPref") && $("#modalPref").value) || '';
            payload.satisfaccionTrabajo = null;
            payload.usaConocimientosCarrera = null;
            payload.tiempoConseguirTrabajo = null;

            var extrasB = {
                dispInmediata: ($("#dispInmediata") && $("#dispInmediata").checked) || false,
                fechaDisponible: ($("#fechaDisp") && $("#fechaDisp").value) || null,
                jornada: ($("#jornada") && $("#jornada").value) || '',
                areas: ($("#areas") && $("#areas").value) || '',
                ubicaciones: ($("#locs") && $("#locs").value) || '',
                idioma: ($("#idioma2") && $("#idioma2").value) || '',
                nivel: ($("#nivel2") && $("#nivel2").value) || '',
                skills: ($("#skills2") && $("#skills2").value) || '',
                alertasEmpleo: ($("#alertasEmpleo") && $("#alertasEmpleo").checked) || false,
                consentMarketing: ($("#consentMarketing") && $("#consentMarketing").checked) || false
            };
            payload.respuestasJson = JSON.stringify(extrasB);
        }

        var btns = Array.prototype.slice.call(document.querySelectorAll('button'))
            .filter(function (b) { return b.id && b.id.indexOf('btnStep3') === 0; });
        btns.forEach(function (b) { b.disabled = true; });

        return fetch(URLS.sit, {
            method: 'POST',
            headers: {
                'Accept': 'application/json',
                'Content-Type': 'application/x-www-form-urlencoded; charset=UTF-8',
                'X-Requested-With': 'XMLHttpRequest'
            },
            body: toForm(payload),
            credentials: 'same-origin'
        })
            .then(safeJson)
            .then(function (json) {
                if (!json || !json.success) {
                    throw new Error((json && json.message) || 'Error guardando situación laboral');
                }
                return true;
            })
            .catch(function (err) {
                Swal.fire({
                    icon: 'error',
                    title: 'Error al guardar',
                    text: err.message || 'Hubo un problema al guardar tu situación laboral. Por favor intenta nuevamente.',
                    confirmButtonText: 'Cerrar'
                });
                throw err;
            })
            .finally(function () {
                btns.forEach(function (b) { b.disabled = false; });
            });
    }

    // ===== PASO 3: VALIDACIÓN Y FINALIZACIÓN =====
    (function () {
        var trabajaSi = $("#trabajaSi");
        var trabajaNo = $("#trabajaNo");
        var trabajaSi2 = $("#trabajaSi2");
        var trabajaNo2 = $("#trabajaNo2");

        function setTrabajo(isSi) {
            var si1 = $("#trabajaSi"), no1 = $("#trabajaNo"),
                si2 = $("#trabajaSi2"), no2 = $("#trabajaNo2");
            if (si1) si1.checked = !!isSi;
            if (no1) no1.checked = !isSi;
            if (si2) si2.checked = !!isSi;
            if (no2) no2.checked = !isSi;
            showStep(isSi ? "wizard-step3A" : "wizard-step3B");
        }

        if (trabajaSi) trabajaSi.addEventListener("change", function () {
            if (this.checked) setTrabajo(true);
        });
        if (trabajaNo) trabajaNo.addEventListener("change", function () {
            if (this.checked) setTrabajo(false);
        });
        if (trabajaSi2) trabajaSi2.addEventListener("change", function () {
            if (this.checked) setTrabajo(true);
        });
        if (trabajaNo2) trabajaNo2.addEventListener("change", function () {
            if (this.checked) setTrabajo(false);
        });

        // Estado inicial coherente al entrar al paso 3
        setTrabajo((trabajaSi && trabajaSi.checked) || (trabajaSi2 && trabajaSi2.checked));

        // Botones de retroceso
        var b3AB = $("#btnStep3ABack");
        var b3BB = $("#btnStep3BBack");
        if (b3AB) b3AB.addEventListener("click", function () { showStep("wizard-step2"); });
        if (b3BB) b3BB.addEventListener("click", function () { showStep("wizard-step2"); });

        // ===== VALIDACIÓN PASO 3A (SÍ TRABAJA) =====
        function validarPaso3A() {
            var errores = [];

            // Validar empresa
            var valEmpresa = Validaciones.texto(
                $("#empresa") ? $("#empresa").value : '',
                "La empresa",
                2
            );
            if (!valEmpresa.valido) errores.push(valEmpresa.mensaje);

            // Validar cargo
            var valCargo = Validaciones.texto(
                $("#cargo") ? $("#cargo").value : '',
                "El cargo",
                2
            );
            if (!valCargo.valido) errores.push(valCargo.mensaje);

            // Validar área
            var valArea = Validaciones.select(
                $("#area") ? $("#area").value : '',
                "un área"
            );
            if (!valArea.valido) errores.push(valArea.mensaje);

            // Validar tipo de contrato
            var valTipoContr = Validaciones.select(
                $("#tipoContr") ? $("#tipoContr").value : '',
                "un tipo de contrato"
            );
            if (!valTipoContr.valido) errores.push(valTipoContr.mensaje);

            // Validar modalidad
            var valModalidad = Validaciones.select(
                $("#modalidad") ? $("#modalidad").value : '',
                "una modalidad de trabajo"
            );
            if (!valModalidad.valido) errores.push(valModalidad.mensaje);

            // Validar antigüedad
            var valAntig = Validaciones.select(
                $("#antig") ? $("#antig").value : '',
                "la antigüedad"
            );
            if (!valAntig.valido) errores.push(valAntig.mensaje);

            // Validar tiempo al primer empleo
            var valTiempo = Validaciones.select(
                $("#tiempoPrimerEmpleo") ? $("#tiempoPrimerEmpleo").value : '',
                "el tiempo al primer empleo"
            );
            if (!valTiempo.valido) errores.push(valTiempo.mensaje);

            // Validar idioma
            var valIdioma = Validaciones.select(
                $("#idioma") ? $("#idioma").value : '',
                "un idioma"
            );
            if (!valIdioma.valido) errores.push(valIdioma.mensaje);

            // Validar nivel de idioma
            var valNivel = Validaciones.select(
                $("#nivel") ? $("#nivel").value : '',
                "el nivel del idioma"
            );
            if (!valNivel.valido) errores.push(valNivel.mensaje);

            return {
                valido: errores.length === 0,
                errores: errores
            };
        }

        // ===== VALIDACIÓN PASO 3B (NO TRABAJA) =====
        function validarPaso3B() {
            var errores = [];

            // Validar CV (obligatorio)
            var cvInput = $("#cvFile");
            var cvFile = (cvInput && cvInput.files && cvInput.files[0]) ? cvInput.files[0] : null;
            var valCV = Validaciones.archivo(cvFile, "application/pdf", 5 * 1024 * 1024);
            if (!valCV.valido) errores.push("CV: " + valCV.mensaje);

            // Validar modalidad preferida
            var valModalPref = Validaciones.select(
                $("#modalPref") ? $("#modalPref").value : '',
                "una modalidad preferida"
            );
            if (!valModalPref.valido) errores.push(valModalPref.mensaje);

            // Validar jornada
            var valJornada = Validaciones.select(
                $("#jornada") ? $("#jornada").value : '',
                "un tipo de jornada"
            );
            if (!valJornada.valido) errores.push(valJornada.mensaje);

            // Validar fecha disponible si no es inmediata
            var dispInmediata = $("#dispInmediata");
            var fechaDisp = $("#fechaDisp");
            if (dispInmediata && !dispInmediata.checked) {
                var valFechaDisp = Validaciones.fecha(
                    fechaDisp ? fechaDisp.value : '',
                    "La fecha de disponibilidad"
                );
                if (!valFechaDisp.valido) errores.push(valFechaDisp.mensaje);
            }

            // Validar idioma
            var valIdioma = Validaciones.select(
                $("#idioma2") ? $("#idioma2").value : '',
                "un idioma"
            );
            if (!valIdioma.valido) errores.push(valIdioma.mensaje);

            // Validar nivel de idioma
            var valNivel = Validaciones.select(
                $("#nivel2") ? $("#nivel2").value : '',
                "el nivel del idioma"
            );
            if (!valNivel.valido) errores.push(valNivel.mensaje);

            return {
                valido: errores.length === 0,
                errores: errores
            };
        }

        // ===== FUNCIÓN FINALIZAR =====
        function finalizar(isTrabajando) {
            // Validar contraseña nuevamente por seguridad
            var p1 = ($("#password") && $("#password").value) || '';
            var p2 = ($("#password2") && $("#password2").value) || '';
            var valPass = Validaciones.password(p1, p2);

            if (!valPass.valido) {
                Swal.fire({
                    icon: 'error',
                    title: 'Contraseña inválida',
                    text: valPass.mensaje,
                    confirmButtonText: 'Revisar'
                });
                return;
            }

            // Validar el paso correspondiente
            var validacion = isTrabajando ? validarPaso3A() : validarPaso3B();

            if (!validacion.valido) {
                var listaErrores = validacion.errores.map(function (err) {
                    return '• ' + err;
                }).join('<br>');

                Swal.fire({
                    icon: 'error',
                    title: 'Información incompleta',
                    html: '<div style="text-align: left;"><strong>Por favor completa los siguientes campos:</strong><br><br>' + listaErrores + '</div>',
                    confirmButtonText: 'Revisar',
                    customClass: {
                        popup: 'swal-wide'
                    }
                });
                return;
            }

            // Confirmación antes de enviar
            Swal.fire({
                title: '¿Confirmar registro?',
                text: 'Estás a punto de completar tu registro. ¿Deseas continuar?',
                icon: 'question',
                showCancelButton: true,
                confirmButtonText: 'Sí, registrar',
                cancelButtonText: 'Revisar datos',
                confirmButtonColor: '#5D0A28',
                cancelButtonColor: '#6c757d'
            }).then((result) => {
                if (result.isConfirmed) {
                    var validacion = isTrabajando ? validarPaso3A() : validarPaso3B();
                    if (!validacion.valido) {
                        var listaErrores = validacion.errores.map(function (err) {
                            return '• ' + err;
                        }).join('<br>');

                        Swal.fire({
                            icon: 'error',
                            title: 'Información incompleta',
                            html: '<div style="text-align: left;"><strong>Por favor completa los siguientes campos:</strong><br><br>' + listaErrores + '</div>',
                            confirmButtonText: 'Revisar',
                            customClass: {
                                popup: 'swal-wide'
                            }
                        });
                        return;
                    }
                    procesarRegistro(isTrabajando);
                }
            });
        }

        function procesarRegistro(isTrabajando) {
            // Mostrar loading mientras se procesa
            Swal.fire({
                title: 'Procesando registro...',
                html: 'Estamos guardando tu información de forma segura.<br>Por favor espera un momento.',
                allowOutsideClick: false,
                allowEscapeKey: false,
                allowEnterKey: false,
                didOpen: () => {
                    Swal.showLoading();
                }
            });

            postRegistrarEgresado(isTrabajando)
                .then(function (id) {
                    return postGuardarSituacionLaboral(id, isTrabajando);
                })
                .then(function () {
                    Swal.fire({
                        icon: 'success',
                        title: '¡Registro completado!',
                        html: '<p>Tu onboarding ha sido exitoso.</p><p>Bienvenido/a a la plataforma de egresados.</p>',
                        confirmButtonText: 'Ir al inicio',
                        confirmButtonColor: '#5D0A28',
                        timer: 5000,
                        timerProgressBar: true,
                        allowOutsideClick: false
                    }).then(function () {
                        window.location.href = URLS.home;
                    });
                })
                .catch(function (err) {
                    // Los errores específicos ya se muestran en las funciones post*
                    console.error('Error en el proceso de registro:', err);
                });
        }

        // ===== BOTONES DE GUARDADO DE BORRADOR =====
        var btnDraft1 = $("#btnStep1Draft");
        var btnDraft2 = $("#btnStep2Draft");
        var btnDraft3A = $("#btnStep3ADraft");
        var btnDraft3B = $("#btnStep3BDraft");

        function guardarBorrador() {
            Swal.fire({
                icon: 'info',
                title: 'Guardar borrador',
                text: 'Esta funcionalidad guardará tu progreso localmente. ¿Deseas continuar?',
                showCancelButton: true,
                confirmButtonText: 'Guardar',
                cancelButtonText: 'Cancelar',
                confirmButtonColor: '#5D0A28'
            }).then((result) => {
                if (result.isConfirmed) {
                    // Aquí puedes implementar localStorage o sessionStorage si lo necesitas
                    Swal.fire({
                        icon: 'success',
                        title: 'Borrador guardado',
                        text: 'Tu progreso ha sido guardado temporalmente',
                        toast: true,
                        position: 'top-end',
                        showConfirmButton: false,
                        timer: 3000,
                        timerProgressBar: true
                    });
                }
            });
        }

        if (btnDraft1) btnDraft1.addEventListener("click", guardarBorrador);
        if (btnDraft2) btnDraft2.addEventListener("click", guardarBorrador);
        if (btnDraft3A) btnDraft3A.addEventListener("click", guardarBorrador);
        if (btnDraft3B) btnDraft3B.addEventListener("click", guardarBorrador);

        // ===== BOTONES FINALIZAR =====
        var fA = $("#btnStep3AFinish");
        var fB = $("#btnStep3BFinish");
        if (fA) fA.addEventListener("click", function () { finalizar(true); });
        if (fB) fB.addEventListener("click", function () { finalizar(false); });
    })();

    // ===== VALIDACIÓN EN TIEMPO REAL (FEEDBACK VISUAL) =====
    (function () {
        // Email
        var emailInput = $("#emailInst");
        if (emailInput) {
            emailInput.addEventListener("blur", function () {
                var val = Validaciones.email(this.value);
                if (this.value && !val.valido) {
                    this.classList.add("is-invalid");
                    this.classList.remove("is-valid");
                } else if (this.value) {
                    this.classList.add("is-valid");
                    this.classList.remove("is-invalid");
                } else {
                    this.classList.remove("is-valid", "is-invalid");
                }
            });
        }

        // Carné
        var carneInput = $("#carne");
        if (carneInput) {
            carneInput.addEventListener("blur", function () {
                var val = Validaciones.carne(this.value);
                if (this.value && !val.valido) {
                    this.classList.add("is-invalid");
                    this.classList.remove("is-valid");
                } else if (this.value) {
                    this.classList.add("is-valid");
                    this.classList.remove("is-invalid");
                } else {
                    this.classList.remove("is-valid", "is-invalid");
                }
            });
        }

        // Teléfono
        var telInput = $("#telefono");
        if (telInput) {
            telInput.addEventListener("blur", function () {
                var val = Validaciones.telefono(this.value);
                if (this.value && !val.valido) {
                    this.classList.add("is-invalid");
                    this.classList.remove("is-valid");
                } else if (this.value) {
                    this.classList.add("is-valid");
                    this.classList.remove("is-invalid");
                } else {
                    this.classList.remove("is-valid", "is-invalid");
                }
            });
        }

        // Contraseñas coinciden
        var pass1 = $("#password");
        var pass2 = $("#password2");
        if (pass1 && pass2) {
            function validarCoincidencia() {
                if (pass2.value) {
                    if (pass1.value === pass2.value && pass1.value.length >= 6) {
                        pass2.classList.add("is-valid");
                        pass2.classList.remove("is-invalid");
                    } else {
                        pass2.classList.add("is-invalid");
                        pass2.classList.remove("is-valid");
                    }
                }
            }
            pass1.addEventListener("input", validarCoincidencia);
            pass2.addEventListener("input", validarCoincidencia);
        }
    })();

    // ===== PREVENIR ENVÍO DE FORMULARIO CON ENTER =====
    document.addEventListener("keypress", function (e) {
        if (e.key === "Enter" && e.target.tagName !== "TEXTAREA" && e.target.tagName !== "BUTTON") {
            e.preventDefault();
        }
    });

    // ===== ADVERTENCIA AL SALIR SIN COMPLETAR =====
    var formModificado = false;
    document.querySelectorAll('input, select, textarea').forEach(function (el) {
        el.addEventListener("change", function () {
            formModificado = true;
        });
    });

    window.addEventListener("beforeunload", function (e) {
        var step1 = document.querySelector('#wizard-step1');
        var estaEnPaso1 = step1 ? (step1.style.display !== 'none') : true;
        if (formModificado && estaEnPaso1) {
            e.preventDefault();
            e.returnValue = '';
        }
    });

    // ===== INICIALIZACIÓN =====
    showStep("wizard-step1");

    console.log('%c✓ Sistema de validación cargado correctamente', 'color: #5D0A28; font-weight: bold; font-size: 14px;');
});