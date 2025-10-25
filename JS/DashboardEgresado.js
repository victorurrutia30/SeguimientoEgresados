(function () {
    // Helpers DOM
    function qs(sel, root) { return (root || document).querySelector(sel); }
    function qsa(sel, root) { return Array.from((root || document).querySelectorAll(sel)); }

    const endpointsEl = qs('#endpoints');
    const EP = {
        resumen: endpointsEl.dataset.resumen,
        cvcard: endpointsEl.dataset.cvcard,
        matching: endpointsEl.dataset.matching,
        procesos: endpointsEl.dataset.procesos,
        notifs: endpointsEl.dataset.notifs,
        encuesta: endpointsEl.dataset.encuesta,
        evals: endpointsEl.dataset.evals,
        vieron: endpointsEl.dataset.vieron,
        togglecv: endpointsEl.dataset.togglecv,
        privacidad: endpointsEl.dataset.privacidad,
        markread: endpointsEl.dataset.markread,
        markall: endpointsEl.dataset.markall
    };

    function getToken() {
        const el = document.querySelector('#dashboardForm input[name="__RequestVerificationToken"]');
        return el ? el.value : '';
    }

    async function getJSON(url) {
        const res = await fetch(url, { credentials: 'same-origin' });
        return res.json();
    }

    async function postForm(url, data) {
        const form = new URLSearchParams();
        Object.keys(data || {}).forEach(k => form.append(k, data[k]));
        form.append('__RequestVerificationToken', getToken());
        const res = await fetch(url, {
            method: 'POST',
            headers: { 'Content-Type': 'application/x-www-form-urlencoded; charset=UTF-8' },
            body: form,
            credentials: 'same-origin'
        });
        return res.json();
    }

    // -------- UI helpers
    function renderStars(container, score /* 0..10 */, totalEstrellas) {
        const val10 = Number(score || 0);
        const five = Math.max(0, Math.min(5, Math.round((val10 / 10) * 5)));
        const full = '★'.repeat(five);
        const empty = '★'.repeat(5 - five);
        container.innerHTML = `<span class="star">${full}</span><span class="star empty">${empty}</span>`;
        const totalEl = qs('#totalEstrellasText');
        if (totalEl) totalEl.textContent = totalEstrellas ?? 0;
    }

    function fmtDate(d) {
        if (!d) return '-';
        const dt = new Date(d);
        if (isNaN(dt.getTime())) return '-';
        return dt.toLocaleDateString('es-SV', { day: '2-digit', month: 'short', year: 'numeric' });
    }

    function estadoBadgeClass(estado, contratado) {
        if (contratado) return 'status-contratado';
        if (estado === 'En_Revision') return 'status-revision';
        if (estado === 'Entrevista_Tecnica') return 'status-entrevista';
        return '';
    }

    // -------- Loaders
    async function loadResumen() {
        const r = await getJSON(EP.resumen);
        if (!r.ok || !r.data) return;

        const d = r.data;
        const nameEl = qs('#welcomeName');
        if (nameEl) nameEl.textContent = d.NombreCompleto || 'Egresado';

        const parts = [];
        if (d.Carrera) parts.push(`Egresado de ${d.Carrera}`);
        if (d.Facultad) parts.push(d.Facultad);
        if (d.FechaGraduacion) parts.push(`Graduado en ${fmtDate(d.FechaGraduacion)}`);
        const sub = qs('#welcomeSubtitle');
        if (sub) sub.textContent = parts.join(' • ');

        const punt = Number(d.PuntuacionGlobal ?? 0);
        const puntEl = qs('#statPuntVal');
        if (puntEl) puntEl.textContent = punt.toFixed(1);
        renderStars(qs('#starsContainer'), punt, d.TotalEstrellas);
        const nivelEl = qs('#nivelExpText');
        if (nivelEl) nivelEl.textContent = d.NivelExperiencia || '';

        const appsEl = qs('#statAplicacionesVal');
        if (appsEl) appsEl.textContent = d.TotalAplicaciones ?? 0;

        const hiresEl = qs('#statContratacionesVal');
        if (hiresEl) hiresEl.textContent = d.TotalContrataciones ?? 0;
    }

    async function loadEncuesta() {
        const r = await getJSON(EP.encuesta);
        if (!r.ok) return;
        const e = r.data || {};
        qs('#empleadoActualmente').textContent = e.TrabajandoActualmente ? '✓ Sí' : '—';
        const sat = e.SatisfaccionTrabajo || 0;
        qs('#satisfaccionStars').innerHTML =
            `<span class="star">${'★'.repeat(sat)}</span><span class="star empty">${'★'.repeat(Math.max(0, 5 - sat))}</span>`;
        qs('#satisfaccionNumber').textContent = `${sat}/5`;
        qs('#empresaActual').textContent = e.EmpresaActual || '—';
        qs('#usaCarrera').textContent = e.UsaConocimientosCarrera ? 'Sí' : 'No';
        qs('#cargoActual').textContent = e.CargoActual || '—';
        qs('#modalidadTrabajo').textContent = e.ModalidadTrabajo || '—';
        qs('#fechaEncuesta').textContent = fmtDate(e.FechaEncuesta);
    }

    async function loadCvCard() {
        const r = await getJSON(EP.cvcard);
        if (!r.ok) return;
        const cv = r.data || {};
        const viewsEl = qs('#cvViewsNumber');
        if (viewsEl) viewsEl.textContent = cv.VecesVisualizado ?? 0;
        // toggle
        const toggle = qs('#cvToggle');
        if (toggle) {
            if (cv.DisponibleBusqueda) toggle.classList.remove('inactive');
            else toggle.classList.add('inactive');
            toggle.setAttribute('aria-checked', cv.DisponibleBusqueda ? 'true' : 'false');
        }
        // privacidad
        const ddl = qs('#ddlPrivacidad');
        if (ddl && cv.Privacidad) ddl.value = cv.Privacidad;
    }

    async function loadEmpresasQueVieron() {
        const r = await getJSON(EP.vieron);
        if (!r.ok) return;
        const list = r.data || [];
        const el = qs('#empresasVieronCount');
        if (el) el.textContent = list.length || 0;
    }

    async function loadProcesos() {
        const r = await getJSON(EP.procesos);
        if (!r.ok) return;
        const rows = (r.data || []).map(p => {
            const fecha = p.FechaActualizacion || p.FechaInicio;
            const cls = estadoBadgeClass(p.EstadoProceso, p.Contratado);
            return `
        <tr data-empresa="${p.Empresa || ''}" data-id="${p.IdProceso}">
          <td>
            <div class="company-cell">
              <span class="company-name">${p.Empresa || '-'}</span>
            </div>
          </td>
          <td>${p.TituloVacante || '-'}</td>
          <td><span class="status-badge ${cls}">${p.EstadoProceso || '-'}</span></td>
          <td>${fmtDate(fecha)}</td>
          <td><button class="action-btn btnVerProceso" type="button">Ver detalles</button></td>
        </tr>`;
        }).join('');
        const body = qs('#procesosBody');
        if (body) body.innerHTML = rows;
        qsa('.btnVerProceso').forEach(btn => btn.addEventListener('click', (ev) => {
            ev.stopPropagation();
            const tr = ev.target.closest('tr');
            showProcesoModal(tr?.dataset?.empresa || '-', tr?.dataset?.id || '-');
        }));
    }

    async function loadNotificaciones() {
        const r = await getJSON(EP.notifs);
        if (!r.ok) return;
        const items = r.data || [];
        window.__NOTIFS = items;
        const unread = items.filter(n => !n.Leida);
        const badge = qs('#notifBadge');
        if (!badge) return;
        if (unread.length > 0) {
            badge.textContent = String(unread.length);
            badge.style.display = 'flex';
        } else {
            badge.style.display = 'none';
        }
    }

    // -------- Acciones
    async function toggleCV() {
        const toggle = qs('#cvToggle');
        const willEnable = toggle.classList.contains('inactive'); // si está inactivo, lo activaremos
        const res = await postForm(EP.togglecv, { disponible: willEnable });
        if (res && res.ok) {
            toggle.classList.toggle('inactive', !willEnable);
            toggle.setAttribute('aria-checked', willEnable ? 'true' : 'false');
            showModal(willEnable ? '✅ CV Visible' : '🔒 CV Oculto',
                willEnable ? 'Tu CV ahora está visible para empresas.' : 'Tu CV ahora está oculto.');
        } else {
            showModal('⚠️ Error', 'No se pudo actualizar la disponibilidad del CV.');
        }
    }

    async function cambiarPrivacidad(val) {
        const res = await postForm(EP.privacidad, { nivel: val });
        if (res && res.ok) {
            showModal('🔐 Privacidad actualizada', `Nuevo nivel: <strong>${val}</strong>`);
        } else {
            showModal('⚠️ Error', 'No se pudo cambiar la privacidad.');
        }
    }

    async function marcarNotificacionLeida(id) {
        const res = await postForm(EP.markread, { idNotificacion: id });
        if (res && res.ok) await loadNotificaciones();
    }

    async function marcarTodasLeidas() {
        const res = await postForm(EP.markall, {});
        if (res && res.ok) {
            await loadNotificaciones();
            showModal('Listo', `Se actualizaron ${res.updated} notificaciones.`);
        }
    }

    // -------- Modales
    function showModal(title, contentHTML) {
        const modal = qs('#modal');
        qs('#modalHeader').textContent = title || '';
        qs('#modalBody').innerHTML = contentHTML || '';
        modal.classList.add('active');
        modal.setAttribute('aria-hidden', 'false');
    }
    function closeModal() {
        const modal = qs('#modal');
        modal.classList.remove('active');
        modal.setAttribute('aria-hidden', 'true');
    }

    function showNotificationsModal() {
        const items = window.__NOTIFS || [];
        const html = `
      <div style="display:flex;justify-content:space-between;align-items:center;margin-bottom:12px;">
        <strong>🔔 Notificaciones</strong>
        <button class="action-btn" id="btnMarkAll" type="button">Marcar todas como leídas</button>
      </div>
      <ul style="list-style:none;padding:0;max-height:50vh;overflow:auto;">
        ${items.length === 0 ? '<li style="padding:10px;color:#666;">No tienes notificaciones</li>' : items.map(n => `
          <li style="padding:10px;border-bottom:1px solid #eee;display:flex;justify-content:space-between;gap:8px;align-items:center;">
            <div>
              <div style="font-weight:600;">${n.Titulo || '-'}</div>
              <div style="font-size:13px;color:#666;">${n.Mensaje || ''}</div>
              <div style="font-size:12px;color:#999;margin-top:4px;">${fmtDate(n.Fecha)}</div>
            </div>
            ${n.Leida ? '' : `<button class="action-btn btnMarkRead" data-id="${n.IdNotificacion}" type="button">Marcar leída</button>`}
          </li>
        `).join('')}
      </ul>`;
        showModal('Notificaciones', html);
        const btnAll = qs('#btnMarkAll');
        if (btnAll) btnAll.addEventListener('click', marcarTodasLeidas);
        qsa('.btnMarkRead').forEach(b => b.addEventListener('click', async (ev) => {
            const id = ev.currentTarget.dataset.id;
            await marcarNotificacionLeida(id);
            showNotificationsModal();
        }));
    }

    function showProcesoModal(empresa, idProceso) {
        showModal(`📋 Proceso: ${empresa}`, `
      <div style="background:#f8f8f8;padding:15px;border-radius:6px;line-height:1.8;">
        <p><strong>Empresa:</strong> ${empresa}</p>
        <p><strong>Id Proceso:</strong> ${idProceso}</p>
        <p style="font-size:13px;color:#666;">Para ver más detalles, ve a "Mis Aplicaciones".</p>
      </div>
    `);
    }

    function animateCards() {
        const cards = qsa('.stat-card, .card');
        cards.forEach((card, index) => {
            setTimeout(() => {
                card.style.opacity = '0';
                card.style.transform = 'translateY(20px)';
                card.style.transition = 'all 0.5s ease';
                setTimeout(() => {
                    card.style.opacity = '1';
                    card.style.transform = 'translateY(0)';
                }, 50);
            }, index * 90);
        });
    }

    // -------- Bindings
    function bindEvents() {
        const bell = qs('#notifBell');
        if (bell) bell.addEventListener('click', showNotificationsModal);

        const close = qs('#btnModalClose');
        if (close) close.addEventListener('click', closeModal);

        const modal = qs('#modal');
        if (modal) modal.addEventListener('click', function (e) { if (e.target === this) closeModal(); });

        const toggle = qs('#cvToggle');
        if (toggle) {
            toggle.addEventListener('click', toggleCV);
            toggle.addEventListener('keydown', (e) => {
                if (e.key === 'Enter' || e.key === ' ') { e.preventDefault(); toggleCV(); }
            });
        }

        const ddl = qs('#ddlPrivacidad');
        if (ddl) ddl.addEventListener('change', () => cambiarPrivacidad(ddl.value));

        const btnTodos = qs('#btnVerTodos');
        if (btnTodos) btnTodos.addEventListener('click', () => window.location.href = '/Aplicaciones');
    }

    async function refreshAll() {
        await Promise.all([
            loadResumen(),
            loadEncuesta(),
            loadCvCard(),
            loadEmpresasQueVieron(),
            loadProcesos(),
            loadNotificaciones()
        ]);
    }

    // Init
    window.addEventListener('DOMContentLoaded', async () => {
        bindEvents();
        animateCards();
        await refreshAll();
    });
})();
