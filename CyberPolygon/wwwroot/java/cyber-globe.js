window.CyberGlobe = {
    init: function (containerId) {
        const container = document.getElementById(containerId);
        if (!container || typeof THREE === 'undefined') return;

        while (container.firstChild) container.removeChild(container.firstChild);

        const width = container.clientWidth || 700;
        const height = container.clientHeight || 380;

        const scene = new THREE.Scene();
        scene.background = new THREE.Color(0x070b14);

        const camera = new THREE.PerspectiveCamera(45, width / height, 0.1, 1000);
        camera.position.z = 4.5;

        const renderer = new THREE.WebGLRenderer({ antialias: true, alpha: true });
        renderer.setSize(width, height);
        renderer.setPixelRatio(Math.min(window.devicePixelRatio, 2));
        container.appendChild(renderer.domElement);

        const ambient = new THREE.AmbientLight(0x4ec3d1, 0.4);
        scene.add(ambient);

        const pointLight = new THREE.PointLight(0x4ec3d1, 1.5, 25);
        pointLight.position.set(4, 3, 5);
        scene.add(pointLight);

        const planetGroup = new THREE.Group();
        scene.add(planetGroup);

        const geometry = new THREE.SphereGeometry(1.3, 64, 64);
        const material = new THREE.MeshPhongMaterial({
            color: 0x0a1a2a,
            emissive: 0x0d2a3a,
            emissiveIntensity: 0.45,
            shininess: 40,
            transparent: true,
            opacity: 0.95
        });
        planetGroup.add(new THREE.Mesh(geometry, material));

        const wireGeo = new THREE.WireframeGeometry(geometry);
        const wireMat = new THREE.LineBasicMaterial({ color: 0x4ec3d1, transparent: true, opacity: 0.38 });
        planetGroup.add(new THREE.LineSegments(wireGeo, wireMat));

        const atmosGeo = new THREE.SphereGeometry(1.45, 32, 32);
        const atmosMat = new THREE.MeshBasicMaterial({
            color: 0x4ec3d1,
            transparent: true,
            opacity: 0.09,
            side: THREE.BackSide
        });
        planetGroup.add(new THREE.Mesh(atmosGeo, atmosMat));

        function createOrbit(radius, tiltX, speed, color) {
            const curve = new THREE.EllipseCurve(0, 0, radius, radius, 0, Math.PI * 2, false, 0);
            const points = curve.getPoints(128);
            const geo = new THREE.BufferGeometry().setFromPoints(points);
            const mat = new THREE.LineBasicMaterial({ color, transparent: true, opacity: 0.5 });
            const line = new THREE.Line(geo, mat);
            line.rotation.x = tiltX;
            line.userData.speed = speed;
            planetGroup.add(line);

            const dot = new THREE.Mesh(
                new THREE.SphereGeometry(0.038, 8, 8),
                new THREE.MeshBasicMaterial({ color })
            );
            dot.position.set(radius, 0, 0);
            line.add(dot);
            return line;
        }

        const orbit1 = createOrbit(1.9, Math.PI / 2.35, 0.0045, 0x4ec3d1);
        const orbit2 = createOrbit(2.22, Math.PI / 2.7, -0.0032, 0x5ba3e3);
        const orbit3 = createOrbit(1.68, Math.PI / 2.15, 0.0058, 0x7ec8f8);

        const starsGeo = new THREE.BufferGeometry();
        const positions = new Float32Array(700 * 3);
        for (let i = 0; i < 700 * 3; i++) positions[i] = (Math.random() - 0.5) * 40;
        starsGeo.setAttribute('position', new THREE.BufferAttribute(positions, 3));
        scene.add(new THREE.Points(starsGeo, new THREE.PointsMaterial({
            color: 0xb8d4ff, size: 0.04, transparent: true, opacity: 0.7
        })));

        let frameId = null;
        function animate() {
            frameId = requestAnimationFrame(animate);
            planetGroup.rotation.y += 0.0038;
            orbit1.rotation.z += orbit1.userData.speed;
            orbit2.rotation.z += orbit2.userData.speed;
            orbit3.rotation.z += orbit3.userData.speed;
            renderer.render(scene, camera);
        }
        animate();

        function onResize() {
            const w = container.clientWidth;
            const h = container.clientHeight;
            if (!w || !h) return;
            camera.aspect = w / h;
            camera.updateProjectionMatrix();
            renderer.setSize(w, h);
        }
        window.addEventListener('resize', onResize);

        container._globeCleanup = () => {
            if (frameId) cancelAnimationFrame(frameId);
            window.removeEventListener('resize', onResize);
            renderer.dispose();
        };
    },

    destroy: function (containerId) {
        const container = document.getElementById(containerId);
        if (container?._globeCleanup) {
            container._globeCleanup();
            while (container.firstChild) container.removeChild(container.firstChild);
        }
    }
};