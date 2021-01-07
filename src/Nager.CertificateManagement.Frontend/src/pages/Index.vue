<template>
  <q-page padding>
    <CertificateJobAdd @created="refresh" />

    <CertificateJobList ref="list" />
  </q-page>
</template>

<script>
export default {
  name: 'PageIndex',
  components: {
    CertificateJobAdd: () => import('../components/CertificateJobAdd'),
    CertificateJobList: () => import('../components/CertificateJobList')
  },
  methods: {
    async refresh () {
      this.$refs.list.getAll()
    }
  }
}
</script>
